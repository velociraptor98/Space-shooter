#!/usr/bin/env python3
"""Draws the hand-authored bullet-hell sprites: enemy bullets, the player's hitbox marker and aiming crosshair.

Each sprite is a small pixel map using the shared Sweetie 16 palette from retro_sprites.py, so they
sit in the same colour set as everything else. Enemy bullets use warm, bright colours with a dark-to-
light rim so they read clearly over the calm navy background.

Usage: python3 Tools/RetroArt/bullet_sprites.py
"""
import os

from PIL import Image

from retro_sprites import PALETTE, ROOT

OUT_DIR = os.path.join(ROOT, "Assets/Sprites/Bullets")
KEYS = {".": None, "r": "red", "o": "orange", "y": "yellow", "w": "white", "g": "green", "l": "lime", "c": "cyan"}

SPRITES = {
    # Gunship rings: big, slow, unmissable.
    "Orb": [
        "..rrrr..",
        ".rwwwwr.",
        "rwwwwwwr",
        "rwwwwwwr",
        "rwwwwwwr",
        "rwwwwwwr",
        ".rwwwwr.",
        "..rrrr..",
    ],
    # Scout pellets: small and quick.
    "Pellet": [
        ".oo.",
        "oyyo",
        "oyyo",
        ".oo.",
    ],
    # Heavy spirals: elongated so the stream's direction reads at a glance (rotated to face travel).
    "Rice": [
        ".gg.",
        "gllg",
        "gwwg",
        "gwwg",
        "gwwg",
        "gwwg",
        "gllg",
        ".gg.",
    ],
    # The aiming reticle that replaces the mouse cursor in play.
    "Crosshair": [
        "cc.....cc",
        "c.......c",
        ".........",
        ".........",
        "....w....",
        ".........",
        ".........",
        "c.......c",
        "cc.....cc",
    ],
    # The player's true hitbox, shown while focusing.
    "Hitbox": [
        ".rr.",
        "rwwr",
        "rwwr",
        ".rr.",
    ],
}


def draw(rows):
    image = Image.new("RGBA", (len(rows[0]), len(rows)), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, key in enumerate(row):
            if KEYS[key]:
                image.putpixel((x, y), (*PALETTE[KEYS[key]], 255))
    return image


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for name, rows in SPRITES.items():
        draw(rows).save(os.path.join(OUT_DIR, name + ".png"))
        print(f"{name}: {len(rows[0])}x{len(rows)}")


if __name__ == "__main__":
    main()
