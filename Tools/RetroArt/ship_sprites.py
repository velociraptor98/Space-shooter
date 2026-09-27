#!/usr/bin/env python3
"""Builds the enemy ships and the player's pre-rotated sprites.

Enemies: each type is drawn from simple shapes (so its silhouette matches how it attacks), then shaded
and outlined in the shared Sweetie 16 palette. Every ship has two idle frames that pulse its lights.

Player: rotating pixel art on the fly makes its pixels crawl, so the ship is pre-rotated into DIRECTIONS
frames the way 16-bit games did it. Each frame is rotated at 8x resolution and scaled back down by
picking the most common colour in each block, which keeps lines and outlines clean.

Usage: python3 Tools/RetroArt/ship_sprites.py
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw

from retro_sprites import PALETTE, ROOT, SPRITES

ENEMY_DIR = os.path.join(ROOT, "Assets/Sprites/Enemies")
PLAYER_DIR = os.path.join(ROOT, "Assets/Sprites/Player")
PLAYER_SOURCE = os.path.join(ROOT, SPRITES, "Player_Turn_Left/Player Turn Left0000.png")
DIRECTIONS = 64
SUPERSAMPLE = 8

# Regions painted by the shape drawings, each coloured by the ship's scheme below.
EMPTY, HULL, CORE, ACCENT, ENGINE, PORT, PANEL = range(7)


def scheme(hull, core, accent, engine, port):
    # hull: (dark, mid, light); core: (rim, centre). Everything else a single colour.
    return {"hull": [PALETTE[c] for c in hull], "core": [PALETTE[c] for c in core],
            ACCENT: PALETTE[accent], ENGINE: PALETTE[engine], PORT: PALETTE[port]}


def scout(frame):
    # A small, sharp dart: it's fast and fires aimed shots, so it points where it's going.
    image = Image.new("L", (17, 19), EMPTY)
    d = ImageDraw.Draw(image)
    d.polygon([(8, 0), (10, 6), (16, 13), (16, 15), (11, 14), (10, 18), (6, 18), (5, 14), (0, 15), (0, 13), (6, 6)], fill=HULL)
    d.ellipse((7, 4, 9, 10), fill=CORE)
    for x in (0, 15):
        d.rectangle((x, 13, x + 1, 15), fill=ACCENT)
    d.rectangle((6, 17, 7, 18), fill=ENGINE)
    d.rectangle((9, 17, 10, 18), fill=ENGINE)
    colors = scheme(("navy", "teal", "cyan"), ("orange", "yellow"), "orange", "white" if frame else "cyan", "orange")
    return image, colors


def gunship(frame):
    # A round saucer ringed with gun ports: it fires rings, so it looks like it can shoot every way at once.
    size = 27
    image = Image.new("L", (size, size), EMPTY)
    d = ImageDraw.Draw(image)
    c = size // 2
    d.ellipse((0, 0, size - 1, size - 1), fill=HULL)
    d.ellipse((c - 9, c - 9, c + 9, c + 9), outline=PANEL)
    d.ellipse((c - 4, c - 4, c + 4, c + 4), fill=CORE)
    # Eight ports; the second frame steps them round half a port so the ring appears to turn.
    for k in range(8):
        angle = math.radians(k * 45 + (22.5 if frame else 0))
        x, y = c + 11 * math.cos(angle), c + 11 * math.sin(angle)
        d.rectangle((round(x) - 1, round(y) - 1, round(x), round(y)), fill=PORT)
    colors = scheme(("steel", "slate", "silver"), ("red", "white"), "red", "orange", "white" if frame else "red")
    return image, colors


def heavy(frame):
    # A wide fortress with a glowing core: slow and tough, firing the spirals that pour from that core.
    image = Image.new("L", (45, 41), EMPTY)
    d = ImageDraw.Draw(image)
    d.ellipse((10, 3, 34, 38), fill=HULL)
    d.rectangle((8, 15, 36, 27), fill=HULL)
    for x0 in (2, 35):
        d.rounded_rectangle((x0, 9, x0 + 7, 35), radius=3, fill=HULL)
        d.line((x0 + 3, 12, x0 + 3, 31), fill=PANEL)
    # Forward prongs, tipped in accent.
    for x0 in (4, 37):
        d.rectangle((x0, 1, x0 + 3, 9), fill=HULL)
        d.rectangle((x0, 1, x0 + 3, 2), fill=ACCENT)
    d.line((12, 30, 32, 30), fill=PANEL)
    d.ellipse((16, 14, 28, 26) if frame else (17, 15, 27, 25), fill=CORE)
    for box in ((3, 35, 8, 38), (36, 35, 41, 38), (18, 37, 26, 40)):
        d.rectangle(box, fill=ENGINE)
    colors = scheme(("plum", "red", "orange"), ("green", "white" if frame else "lime"), "yellow", "yellow", "yellow")
    return image, colors


def paint(regions, colors):
    # Shade from the shapes: hull edges facing the top of the drawing catch the light, other edges fall
    # into shadow, cores glow brighter in the middle; then a dark outline goes round everything.
    r = np.asarray(regions)
    h, w = r.shape
    padded = np.pad(r, 1)
    above, below = padded[:-2, 1:-1], padded[2:, 1:-1]
    left, right = padded[1:-1, :-2], padded[1:-1, 2:]
    out = np.zeros((h, w, 4), dtype=np.uint8)
    dark, mid, light = colors["hull"]
    hull = r == HULL
    out[hull] = (*mid, 255)
    out[hull & ((below == EMPTY) | (left == EMPTY) | (right == EMPTY))] = (*dark, 255)
    out[hull & (above == EMPTY)] = (*light, 255)
    out[r == PANEL] = (*dark, 255)
    core = r == CORE
    core_edge = core & ((above != CORE) | (below != CORE) | (left != CORE) | (right != CORE))
    out[core] = (*colors["core"][1], 255)
    out[core_edge] = (*colors["core"][0], 255)
    for region in (ACCENT, ENGINE, PORT):
        out[r == region] = (*colors[region], 255)
    return outline(out)


def outline(rgba):
    rgba = np.pad(rgba, ((1, 1), (1, 1), (0, 0)))
    solid = rgba[..., 3] > 0
    grown = solid.copy()
    grown[1:] |= solid[:-1]
    grown[:-1] |= solid[1:]
    grown[:, 1:] |= solid[:, :-1]
    grown[:, :-1] |= solid[:, 1:]
    rgba[grown & ~solid] = (*PALETTE["ink"], 255)
    return rgba


def majority_downscale(image, factor):
    # Each output pixel takes the most common colour (including "transparent") of its factor x factor block.
    pixels = np.asarray(image)
    h, w = pixels.shape[0] // factor, pixels.shape[1] // factor
    pixels = pixels[:h * factor, :w * factor]
    pixels = np.where(pixels[..., 3:] > 0, pixels, 0)
    colors, index = np.unique(pixels.reshape(-1, 4), axis=0, return_inverse=True)
    blocks = index.reshape(h, factor, w, factor).transpose(0, 2, 1, 3).reshape(h, w, -1)
    counts = np.stack([(blocks == k).sum(-1) for k in range(len(colors))], -1)
    return Image.fromarray(colors[counts.argmax(-1)].astype(np.uint8), "RGBA")


def rotate_pixel_art(image, degrees):
    big = image.resize((image.width * SUPERSAMPLE, image.height * SUPERSAMPLE), Image.Resampling.NEAREST)
    big = big.rotate(degrees, resample=Image.Resampling.NEAREST)
    return majority_downscale(big, SUPERSAMPLE)


def main():
    os.makedirs(ENEMY_DIR, exist_ok=True)
    for name, build in (("Scout", scout), ("Gunship", gunship), ("Heavy", heavy)):
        for frame in (0, 1):
            regions, colors = build(frame)
            # Drawn nose-up; enemy sprites face down the screen, the way they fly in from the top.
            sprite = Image.fromarray(paint(regions, colors), "RGBA").transpose(Image.Transpose.FLIP_TOP_BOTTOM)
            sprite.save(os.path.join(ENEMY_DIR, f"{name}_{frame}.png"))
        print(f"{name}: {sprite.width}x{sprite.height}")

    os.makedirs(PLAYER_DIR, exist_ok=True)
    base = Image.open(PLAYER_SOURCE).convert("RGBA")
    for i in range(DIRECTIONS):
        # Frame i faces i steps anticlockwise from straight up, matching Unity's z rotation.
        rotate_pixel_art(base, i * 360.0 / DIRECTIONS).save(os.path.join(PLAYER_DIR, f"Player_{i:02d}.png"))
    print(f"Player: {DIRECTIONS} directions at {base.width}x{base.height}")


if __name__ == "__main__":
    main()
