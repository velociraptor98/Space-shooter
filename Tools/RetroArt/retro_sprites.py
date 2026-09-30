#!/usr/bin/env python3
"""Regenerates the game's sprites in a 16-bit retro style from the original high-res art.

The originals are read straight from git (SOURCE_COMMIT), so this can be re-run and re-tuned at
any time without keeping a second copy of the source art in the repo.

Per sprite group it:
  1. Downscales to the shared retro pixel grid (RETRO_PPU texels per world unit, taking the
     object's in-game scale into account) using premultiplied area averaging.
  2. Maps colours onto a handful of entries from one shared 16-colour palette (Sweetie 16), with
     brightness stretched to use that group's full range. Every sprite shares the same palette, so
     the whole game reads as one set; a whole animation shares one tone mapping so frames don't flicker.
  3. Hardens transparency (1-bit for sprites, a 50% level for translucent effects), removes
     isolated stray pixels, and optionally adds a dark 1px outline.

Writes the PNGs over the originals in Assets/ (keeping their .meta files, and so their GUIDs) and a
manifest of the pixels-per-unit each texture needs, which apply_import_settings.cs consumes.

Usage: python3 Tools/RetroArt/retro_sprites.py [--preview OUT_DIR]
  --preview writes the converted sprites under OUT_DIR instead of Assets/, for reviewing a tweak.
"""
import argparse
import fnmatch
import io
import json
import os
import subprocess

import numpy as np
from PIL import Image, ImageEnhance, ImageFilter

SOURCE_COMMIT = "1a91cfc"
RETRO_PPU = 17  # The Pixel Perfect Camera's pixels per unit.
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SPRITES = "Assets/2D Galaxy Assets/Game/Sprites"

# Sweetie 16 by GrafxKid (lospec.com/palette-list/sweetie-16). The cool navies, blues and teals set a
# calm base; the warm reds, oranges and yellows are kept for fire, lasers and danger so the action pops.
PALETTE = {
    "ink": (26, 28, 44), "plum": (93, 39, 93), "red": (177, 62, 83), "orange": (239, 125, 87),
    "yellow": (255, 205, 117), "lime": (167, 240, 112), "green": (56, 183, 100), "teal": (37, 113, 121),
    "navy": (41, 54, 111), "blue": (59, 93, 201), "sky": (65, 166, 246), "cyan": (115, 239, 247),
    "white": (244, 244, 244), "silver": (148, 176, 194), "slate": (86, 108, 134), "steel": (51, 60, 87),
}
OUTLINE = PALETTE["ink"]
HULL = ["ink", "steel", "slate", "silver", "white"]
# Flame ramps run from the cool outer edge to the white-hot core.
FIRE = ["plum", "red", "orange", "yellow", "white"]
FIRE_BANDS = [0.30, 0.47, 0.64, 0.82]

# scale: the object's x scale in game, so a texel lands on exactly one retro pixel.
# ui_size: for UI images (drawn by the Canvas, not the world camera), the output size instead.
# colors: the PALETTE entries this group may use.
# alpha: "solid" = 1-bit, "translucent" = 0/50/100%.
# flatten: majority-colour filter for busy, photographic art (rock, smoke) so it reads as flat shading.
# fire: recolour flames with the FIRE ramp - "all" pixels, or only "bright" ones (flames over a hull).
# write: only save matching frames; the rest still set the group's brightness range.
GROUPS = [
    dict(name="player", glob="Player_Turn_*/*.png", write="Player Turn Left0000.png", scale=0.5, colors=HULL + ["sky", "orange"], alpha="solid", outline=True, flatten=True),
    dict(name="explosion", glob="Explosion/*.png", scale=1.0, colors=FIRE, alpha="solid", flatten=True, fire="all"),
    dict(name="engine_fire", glob="Player_Hurt/*.png", scale=0.5, colors=["steel", "slate", "silver"], alpha="solid", fire="bright"),
    dict(name="shield", glob="Player_Shield/*.png", scale=1.0, colors=["blue", "sky", "cyan", "white"], alpha="translucent"),
    dict(name="powerup_triple", glob="Power_Ups/Triple_Shot/*.png", scale=0.5, colors=HULL + ["green", "lime"],
         alpha="solid", outline=True, strip_label=True),
    dict(name="powerup_speed", glob="Power_Ups/Speed/*.psd", scale=0.5, colors=HULL + ["red", "orange"],
         alpha="solid", outline=True, strip_label=True),
    dict(name="powerup_shield", glob="Power_Ups/Shield/*.png", scale=0.5, colors=HULL + ["sky", "cyan"],
         alpha="solid", outline=True, strip_label=True),
    dict(name="laser", glob="laser.png", scale=0.83, colors=["red", "orange", "yellow", "white"], alpha="solid"),
    dict(name="lives", glob="UI/Lives/*.png", ui_size=(64, 32), colors=HULL + ["sky", "orange"], alpha="solid"),
]
# The power-up art has its name printed underneath; at retro size it is an unreadable smear, so the
# colour-coded pod carries the meaning instead. Rows at or below this (in source pixels) are dropped.
LABEL_TOP = 330


def list_originals(pattern):
    listing = subprocess.run(["git", "ls-tree", "-r", "--name-only", SOURCE_COMMIT, "--", SPRITES],
                             cwd=ROOT, check=True, capture_output=True, text=True).stdout.splitlines()
    return sorted(p for p in listing if fnmatch.fnmatchcase(p, f"{SPRITES}/{pattern}"))


def load_original(path):
    data = subprocess.run(["git", "show", f"{SOURCE_COMMIT}:{path}"], cwd=ROOT, check=True, capture_output=True).stdout
    return Image.open(io.BytesIO(data)).convert("RGBA")


def downscale(image, size):
    # Premultiply so transparent pixels don't bleed their (often black) colour into edges.
    return image.convert("RGBa").resize(size, Image.Resampling.BOX).convert("RGBA")


def to_ycc(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return np.stack([0.299 * r + 0.587 * g + 0.114 * b,
                     -0.1687 * r - 0.3313 * g + 0.5 * b,
                     0.5 * r - 0.4187 * g - 0.0813 * b], -1)


def brightness_levels(images):
    # The 2nd-98th percentile brightness of the art, so each group can use its colours' full range.
    luma = np.concatenate([to_ycc(np.asarray(im, dtype=float)[..., :3])[..., 0][np.asarray(im)[..., 3] > 0]
                           for im in images])
    return np.percentile(luma, 2), max(np.percentile(luma, 98), np.percentile(luma, 2) + 1.0)


def map_to_palette(rgb, colors, levels):
    palette = np.array([PALETTE[c] for c in colors], dtype=float)
    target = to_ycc(palette)
    source = to_ycc(rgb.astype(float))
    low, high = levels
    ymin, ymax = target[:, 0].min(), target[:, 0].max()
    source[..., 0] = ymin + np.clip((source[..., 0] - low) / (high - low), 0.0, 1.0) * (ymax - ymin)
    source[..., 1:] *= 1.5  # The art is quite desaturated; let its accents find the accent colours.
    # Brightness matters most for reading shapes, so weight it above hue.
    distances = (2.0 * (source[..., None, 0] - target[None, None, :, 0]) ** 2
                 + ((source[..., None, 1:] - target[None, None, :, 1:]) ** 2).sum(-1))
    return palette[distances.argmin(-1)]


def fire_mask(rgba, mode):
    if mode == "all":
        return np.ones(rgba.shape[:2], dtype=bool)
    r, g, b = (rgba[..., i] / 255.0 for i in range(3))
    luminance = 0.299 * r + 0.587 * g + 0.114 * b
    return (luminance > 0.85) | ((luminance > 0.5) & (r - b > 0.15))


def distance_inside(solid):
    # Steps from the nearest transparent pixel (4-connected), by repeated erosion.
    distance = np.zeros(solid.shape)
    current = solid.copy()
    while current.any():
        distance += current
        eroded = current.copy()
        eroded[1:] &= current[:-1]
        eroded[:-1] &= current[1:]
        eroded[:, 1:] &= current[:, :-1]
        eroded[:, :-1] &= current[:, 1:]
        eroded[0] = eroded[-1] = False
        eroded[:, 0] = eroded[:, -1] = False
        current = eroded
    return distance


def fire_colors(rgba, solid):
    # Band mostly by relative depth into the shape so flames get concentric rings - hot core, cool
    # rim - even where the source art is blown out to flat white - with brightness adding variation.
    rgb = rgba[..., :3] / 255.0
    luminance = (0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]) * (rgba[..., 3] / 255.0)
    distance = distance_inside(solid)
    depth = distance / max(distance.max(), 1.0)
    score = 0.35 * luminance + 0.65 * depth
    ramp = np.array([PALETTE[c] for c in FIRE], dtype=float)
    return ramp[np.digitize(score, FIRE_BANDS)]


def majority_filter(rgb):
    # Mode filter over colour *indices*: filtering the RGB channels separately would mix channels
    # from different neighbours and invent colours that aren't in the palette.
    colors, indices = np.unique(rgb.reshape(-1, 3), axis=0, return_inverse=True)
    index_image = Image.fromarray(indices.reshape(rgb.shape[:2]).astype(np.uint8), "L")
    filtered = np.asarray(index_image.filter(ImageFilter.ModeFilter(3)))
    return colors[filtered]


def remove_strays(rgb, alpha):
    # A pixel unlike all four neighbours reads as noise at this size; give it the most common neighbour colour.
    out = rgb.copy()
    h, w = alpha.shape
    key = (rgb[..., 0] * 65536 + rgb[..., 1] * 256 + rgb[..., 2]).astype(np.int64)
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            if not alpha[y, x]:
                continue
            neighbours = [(y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)]
            keys = [key[n] for n in neighbours if alpha[n]]
            if len(keys) == 4 and key[y, x] not in keys:
                best = max(set(keys), key=keys.count)
                ny, nx = neighbours[keys.index(best)]
                out[y, x] = rgb[ny, nx]
    return out


def add_outline(rgba):
    solid = rgba[..., 3] > 0
    grown = solid.copy()
    grown[1:] |= solid[:-1]
    grown[:-1] |= solid[1:]
    grown[:, 1:] |= solid[:, :-1]
    grown[:, :-1] |= solid[:, 1:]
    edge = grown & ~solid
    rgba[edge] = (*OUTLINE, 255)
    return rgba


def target_path(rel):
    return rel if rel.endswith(".png") else os.path.splitext(rel)[0] + ".png"


def even(value):
    return max(2, int(round(value / 2.0)) * 2)


def process_group(group, preview_dir):
    rel_paths = list_originals(group["glob"])
    small = []
    for rel in rel_paths:
        image = load_original(rel)
        if group.get("strip_label"):
            image.paste((0, 0, 0, 0), (0, LABEL_TOP, image.width, image.height))
        if "ui_size" in group:
            size = group["ui_size"]
        else:
            factor = RETRO_PPU * group["scale"] / 100.0
            size = (even(image.width * factor), even(image.height * factor))
        image = downscale(image, size)
        rgb = ImageEnhance.Contrast(ImageEnhance.Color(image.convert("RGB")).enhance(1.3)).enhance(1.3)
        image = Image.merge("RGBA", (*rgb.split(), image.getchannel("A")))
        small.append(image)

    levels = brightness_levels(small)
    for rel, image in zip(rel_paths, small):
        if not written(group, rel):
            continue
        rgba = np.asarray(image).astype(float)
        alpha = rgba[..., 3]
        if group["alpha"] == "translucent":
            alpha = np.select([alpha < 64, alpha < 176], [0, 128], 255)
        else:
            alpha = np.where(alpha < 128, 0, 255)
        rgb = map_to_palette(rgba[..., :3], group["colors"], levels)
        if group.get("fire"):
            flames = fire_mask(rgba, group["fire"])[..., None]
            rgb = np.where(flames, fire_colors(rgba, alpha > 0), rgb)
        if group.get("flatten"):
            rgb = np.where((alpha > 0)[..., None], majority_filter(rgb), rgb)
        rgb = remove_strays(rgb, alpha > 0)
        out = np.dstack([rgb, alpha]).astype(np.uint8)
        out[out[..., 3] == 0] = 0
        if group.get("outline"):
            out = add_outline(out)

        target = target_path(rel)
        if preview_dir:
            destination = os.path.join(preview_dir, os.path.relpath(target, SPRITES))
            os.makedirs(os.path.dirname(destination), exist_ok=True)
        else:
            destination = os.path.join(ROOT, target)
            os.makedirs(os.path.dirname(destination), exist_ok=True)
            if target != rel and os.path.exists(os.path.join(ROOT, rel)):
                # Unity can't write .psd, so it becomes a .png that keeps the original's .meta (and
                # so its GUID) and every animation that references it survives.
                os.replace(os.path.join(ROOT, rel + ".meta"), os.path.join(ROOT, target + ".meta"))
                os.remove(os.path.join(ROOT, rel))
        Image.fromarray(out, "RGBA").save(destination, optimize=True)
    print(f"{group['name']:>15}: {sum(written(group, rel) for rel in rel_paths):3d} sprites, {len(group['colors'])} colours, {small[0].size[0]}x{small[0].size[1]}")


def written(group, rel):
    return fnmatch.fnmatchcase(os.path.basename(rel), group.get("write", "*"))


def write_manifest():
    # Always lists every group, so a partial --only run can't drop sprites from the import settings.
    sprites = []
    for group in GROUPS:
        ppu = 0 if "ui_size" in group else round(RETRO_PPU * group["scale"], 2)
        for rel in filter(lambda rel: written(group, rel), list_originals(group["glob"])):
            sprites.append({"path": target_path(rel), "ppu": ppu})
    with open(os.path.join(ROOT, "Tools/RetroArt/manifest.json"), "w") as f:
        json.dump({"retroPpu": RETRO_PPU, "sprites": sprites}, f, indent=1)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", metavar="OUT_DIR")
    parser.add_argument("--only", nargs="*", help="Group names to process (default: all)")
    args = parser.parse_args()
    for group in GROUPS:
        if not args.only or group["name"] in args.only:
            process_group(group, args.preview)
    if not args.preview:
        write_manifest()


if __name__ == "__main__":
    main()
