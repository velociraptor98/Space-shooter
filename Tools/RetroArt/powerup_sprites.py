#!/usr/bin/env python3
"""Draws the power-up pickups.

Every pickup shares one silhouette - a chamfered chip with a bevelled rim around a dark window - so it
reads as "collect me" rather than as another ship. What it grants is told twice: the rim colour, and the
icon in the window (a three-way spread, a lightning bolt, a shield crest). A glint sweeps across the
chip now and then to catch the eye, played by SpriteLoop.

Usage: python3 Tools/RetroArt/powerup_sprites.py [--preview OUT_DIR]
"""
import argparse
import os

import numpy as np
from PIL import Image

from retro_sprites import PALETTE, ROOT

OUT_DIR = os.path.join(ROOT, "Assets/Sprites/Powerups")
SIZE = 17
CHAMFER = 4
RIM = 2
FRAMES = 8
# Frames the glint is visible in; the rest of the loop the chip sits still.
GLINT_FRAMES = range(1, 5)

# rim: (dark, mid, light), the dark one also filling the window behind the icon; accent: the icon's colour.
SCHEMES = {
    "TripleShot": dict(rim=("teal", "green", "lime"), accent="lime"),
    "Speed": dict(rim=("plum", "red", "orange"), accent="yellow"),
    "Shield": dict(rim=("navy", "blue", "sky"), accent="cyan"),
}

# 9x9 icons: "w" white, "a" the scheme's accent.
ICONS = {
    # Three shots fanning out from the ship.
    "TripleShot": [
        ".........",
        "a...a...a",
        "w...w...w",
        ".w..w..w.",
        ".w..w..w.",
        "..w.w.w..",
        "..w.w.w..",
        "...www...",
        ".........",
    ],
    "Speed": [
        ".....aaa.",
        "....aaw..",
        "...aaw...",
        "..aaw....",
        ".awwwwww.",
        "....wwa..",
        "...wwa...",
        "..wwa....",
        ".wa......",
    ],
    "Shield": [
        ".wwwwwww.",
        "wwaaaaaww",
        "wwaaaaaww",
        "wwaaaaaww",
        ".wwaaaww.",
        ".wwaaaww.",
        "..wwaww..",
        "...www...",
        "....w....",
    ],
}


def chip_mask():
    # A square with its corners cut off at 45 degrees.
    y, x = np.mgrid[0:SIZE, 0:SIZE]
    far = SIZE - 1
    return ((x + y >= CHAMFER) & (far - x + y >= CHAMFER)
            & (x + far - y >= CHAMFER) & (far - x + far - y >= CHAMFER))


def erode(mask, steps):
    for _ in range(steps):
        inner = mask.copy()
        inner[1:] &= mask[:-1]
        inner[:-1] &= mask[1:]
        inner[:, 1:] &= mask[:, :-1]
        inner[:, :-1] &= mask[:, 1:]
        inner[0] = inner[-1] = False
        inner[:, 0] = inner[:, -1] = False
        mask = inner
    return mask


def draw(name, frame):
    scheme = SCHEMES[name]
    dark, mid, light = (np.array(PALETTE[c]) for c in scheme["rim"])
    chip = chip_mask()
    window = erode(chip, RIM)
    rim = chip & ~window
    y, x = np.mgrid[0:SIZE, 0:SIZE]

    rgb = np.zeros((SIZE, SIZE, 3))
    # Bevel lit from the top left: the rim's outer edge catches the light on that side and is in shadow
    # on the other.
    upper_left = x + y < SIZE - 1
    outer = chip & ~erode(chip, 1)
    rgb[rim] = mid
    rgb[rim & outer & upper_left] = light
    rgb[rim & outer & ~upper_left] = dark
    rgb[window] = dark

    keys = {"w": PALETTE["white"], "a": PALETTE[scheme["accent"]]}
    offset = (SIZE - 9) // 2
    for row, line in enumerate(ICONS[name]):
        for col, key in enumerate(line):
            if key in keys:
                rgb[offset + row, offset + col] = keys[key]

    if frame in GLINT_FRAMES:
        # A two-pixel diagonal band crossing the chip from top left to bottom right.
        step = GLINT_FRAMES.index(frame)
        position = x + y - (step + 1) * (2 * SIZE // (len(GLINT_FRAMES) + 1))
        band = chip & (position >= 0) & (position < 2)
        rgb[band & rim] = PALETTE["white"]
        # Over the window it only lifts the background, leaving the icon untouched.
        rgb[band & window & (rgb == dark).all(-1)] = mid

    rgba = np.dstack([rgb, np.where(chip, 255, 0)]).astype(np.uint8)
    return outline(rgba)


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


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview", metavar="OUT_DIR")
    args = parser.parse_args()
    out_dir = args.preview or OUT_DIR
    os.makedirs(out_dir, exist_ok=True)
    for name in SCHEMES:
        for frame in range(FRAMES):
            Image.fromarray(draw(name, frame), "RGBA").save(os.path.join(out_dir, f"{name}_{frame}.png"))
        print(f"{name}: {FRAMES} frames at {SIZE + 2}x{SIZE + 2}")


if __name__ == "__main__":
    main()
