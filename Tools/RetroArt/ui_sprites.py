#!/usr/bin/env python3
"""Generates the menu art: the ECHO logo, a 5x7 pixel font, the UI frames and the menu blips. Also the
in-game "DODGE!" callout and chime, which are drawn from the same font.

Everything is drawn from hand-made bitmaps in the shared Sweetie 16 palette, so the menus sit in the same
16-bit style as the game. The font atlas is turned into a Unity Font by build_pixel_font.cs, which reads
the glyph table this script writes next to it.

Usage: python3 Tools/RetroArt/ui_sprites.py
"""
import json
import math
import os
import struct
import wave

import numpy as np
from PIL import Image

from retro_sprites import PALETTE, ROOT

UI_DIR = os.path.join(ROOT, "Assets/Sprites/UI")
AUDIO_DIR = os.path.join(ROOT, "Assets/Audio/UI")
GAME_AUDIO_DIR = os.path.join(ROOT, "Assets/Audio")
FONT_TABLE = os.path.join(os.path.dirname(__file__), "pixel_font.json")

# 5x7 glyphs, one string per row. Lower case is drawn with the upper case glyphs.
GLYPHS = {
    "A": [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "B": ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."],
    "C": [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
    "D": ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."],
    "E": ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
    "F": ["#####", "#....", "#....", "####.", "#....", "#....", "#...."],
    "G": [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####"],
    "H": ["#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "I": ["###", ".#.", ".#.", ".#.", ".#.", ".#.", "###"],
    "J": ["..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.."],
    "K": ["#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"],
    "L": ["#....", "#....", "#....", "#....", "#....", "#....", "#####"],
    "M": ["#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"],
    "N": ["#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#"],
    "O": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "P": ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."],
    "Q": [".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"],
    "R": ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
    "S": [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
    "T": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
    "U": ["#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "V": ["#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."],
    "W": ["#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#"],
    "X": ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"],
    "Y": ["#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."],
    "Z": ["#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"],
    "0": [".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."],
    "1": [".#.", "##.", ".#.", ".#.", ".#.", ".#.", "###"],
    "2": [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"],
    "3": ["####.", "....#", "....#", ".###.", "....#", "....#", "####."],
    "4": ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."],
    "5": ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."],
    "6": [".###.", "#....", "#....", "####.", "#...#", "#...#", ".###."],
    "7": ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."],
    "8": [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."],
    "9": [".###.", "#...#", "#...#", ".####", "....#", "....#", ".###."],
    ".": [".", ".", ".", ".", ".", ".", "#"],
    ",": ["..", "..", "..", "..", "..", ".#", "#."],
    ":": [".", ".", "#", ".", ".", "#", "."],
    "!": ["#", "#", "#", "#", "#", ".", "#"],
    "?": [".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.."],
    "'": ["#", "#", ".", ".", ".", ".", "."],
    "-": ["...", "...", "...", "###", "...", "...", "..."],
    "+": [".....", "..#..", "..#..", "#####", "..#..", "..#..", "....."],
    "/": ["....#", "...#.", "...#.", "..#..", ".#...", ".#...", "#...."],
    "%": ["##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##"],
    "(": [".#", "#.", "#.", "#.", "#.", "#.", ".#"],
    ")": ["#.", ".#", ".#", ".#", ".#", ".#", "#."],
    "<": ["...#", "..#.", ".#..", "#...", ".#..", "..#.", "...#"],
    ">": ["#...", ".#..", "..#.", "...#", "..#.", ".#..", "#..."],
    "&": [".##..", "#..#.", "#.#..", ".#...", "#.#.#", "#..#.", ".##.#"],
    "=": [".....", ".....", "#####", ".....", "#####", ".....", "....."],
}
GLYPH_HEIGHT = 7
CELL = 8  # Atlas cell size; each glyph sits in the top-left of its cell.
SPACE_ADVANCE = 4
LINE_HEIGHT = 10

# The logo's letters on a coarse grid, blown up LOGO_BLOCK times.
LOGO_LETTERS = {
    "E": ["########", "########", "##......", "##......", "#######.", "#######.", "##......", "##......", "########", "########"],
    "C": [".#######", "########", "##......", "##......", "##......", "##......", "##......", "##......", "########", ".#######"],
    "H": ["##....##", "##....##", "##....##", "##....##", "########", "########", "##....##", "##....##", "##....##", "##....##"],
    "O": [".######.", "########", "##....##", "##....##", "##....##", "##....##", "##....##", "##....##", "########", ".######."],
}
LOGO_BLOCK = 4
LOGO_GAP = 2  # In grid cells.
# The letters' vertical colour bands, top to bottom, and the echoes trailing behind them (nearest first).
LOGO_BANDS = [(0.0, "white"), (0.35, "cyan"), (0.65, "sky")]
LOGO_ECHOES = [((3, 3), "blue"), ((6, 6), "plum")]


def rgba(name, alpha=255):
    return (*PALETTE[name], alpha)


def grow(mask):
    grown = mask.copy()
    grown[1:] |= mask[:-1]
    grown[:-1] |= mask[1:]
    grown[:, 1:] |= mask[:, :-1]
    grown[:, :-1] |= mask[:, 1:]
    return grown


def bitmap(rows):
    return np.array([[c == "#" for c in row] for row in rows], dtype=bool)


def font_atlas():
    # Glyphs in a 16-wide grid of CELL-sized cells, white with 1-bit alpha so Text tints them.
    chars = sorted(GLYPHS)
    columns = 16
    rows = math.ceil(len(chars) / columns)
    size = 1 << max(columns * CELL - 1, rows * CELL - 1).bit_length()
    atlas = np.zeros((size, size, 4), dtype=np.uint8)
    table = []
    for i, ch in enumerate(chars):
        glyph = bitmap(GLYPHS[ch])
        x, y = (i % columns) * CELL, (i // columns) * CELL
        h, w = glyph.shape
        atlas[y:y + h, x:x + w][glyph] = (255, 255, 255, 255)
        # Unity's texture origin is the bottom-left, so flip y for the UVs.
        table.append(dict(char=ch, x=x, y=size - y - h, w=w, h=h, advance=w + 1))
    Image.fromarray(atlas, "RGBA").save(os.path.join(UI_DIR, "PixelFont.png"))
    with open(FONT_TABLE, "w") as f:
        json.dump(dict(texture="Assets/Sprites/UI/PixelFont.png", size=size, glyphHeight=GLYPH_HEIGHT,
                       lineHeight=LINE_HEIGHT, spaceAdvance=SPACE_ADVANCE, glyphs=table), f, indent=1)
    return len(chars)


def logo():
    letters = [bitmap(LOGO_LETTERS[ch]) for ch in "ECHO"]
    cells_h = letters[0].shape[0]
    cells_w = sum(l.shape[1] for l in letters) + LOGO_GAP * (len(letters) - 1)
    shape = np.zeros((cells_h, cells_w), dtype=bool)
    x = 0
    for letter in letters:
        shape[:, x:x + letter.shape[1]] = letter
        x += letter.shape[1] + LOGO_GAP
    shape = np.kron(shape, np.ones((LOGO_BLOCK, LOGO_BLOCK), dtype=bool))
    h, w = shape.shape
    far = max(dx for (dx, _), _ in LOGO_ECHOES)
    pad = 2
    out = np.zeros((h + far + pad * 2, w + far + pad * 2, 4), dtype=np.uint8)

    def place(mask, dx, dy):
        full = np.zeros(out.shape[:2], dtype=bool)
        full[pad + dy:pad + dy + h, pad + dx:pad + dx + w] = mask
        return full

    # Echoes back to front, each with a dark outline so it separates from the one in front.
    for (dx, dy), color in reversed(LOGO_ECHOES):
        mask = place(shape, dx, dy)
        out[grow(mask)] = rgba("ink")
        out[mask] = rgba(color)
    front = place(shape, 0, 0)
    out[grow(front)] = rgba("ink")
    ys = np.arange(out.shape[0])[:, None].repeat(out.shape[1], axis=1)
    rel = (ys - pad) / h
    for start, color in LOGO_BANDS:
        out[front & (rel >= start)] = rgba(color)
    # A dark scanline through the chrome, with a highlight under it.
    cut = pad + int(h * 0.55)
    out[front & (ys == cut)] = rgba("navy")
    out[front & (ys == cut + 1)] = rgba("cyan")
    Image.fromarray(out, "RGBA").save(os.path.join(UI_DIR, "EchoLogo.png"))
    # The pulses rippling out from the logo: just the letters' outline, in white so the UI can tint it.
    ring = np.zeros_like(out)
    ring[grow(front) & ~front] = (255, 255, 255, 255)
    Image.fromarray(ring, "RGBA").save(os.path.join(UI_DIR, "EchoLogoRing.png"))


def frame(size, border, fill, inner=None):
    # A 9-sliceable box: a 1px border, an optional 1px inner rim, and a fill. Corners are cut for a pixel look.
    img = np.zeros((size, size, 4), dtype=np.uint8)
    img[:] = fill
    img[0, :] = img[-1, :] = img[:, 0] = img[:, -1] = border
    if inner:
        img[1, 1:-1] = img[-2, 1:-1] = img[1:-1, 1] = img[1:-1, -2] = inner
    for y, x in ((0, 0), (0, -1), (-1, 0), (-1, -1)):
        img[y, x] = (0, 0, 0, 0)
    return Image.fromarray(img, "RGBA")


def ui_sprites():
    frame(12, rgba("sky"), rgba("ink", 235), rgba("navy")).save(os.path.join(UI_DIR, "Panel.png"))
    frame(8, rgba("cyan"), rgba("navy")).save(os.path.join(UI_DIR, "ButtonSelected.png"))
    # Volume bars are a row of these segments, tiled: 3px block and a 1px gap.
    segment = np.zeros((6, 4, 4), dtype=np.uint8)
    segment[:, :3] = (255, 255, 255, 255)
    Image.fromarray(segment, "RGBA").save(os.path.join(UI_DIR, "BarSegment.png"))
    # The selection cursor: a small ship-like chevron.
    cursor = bitmap(["#....", "##...", "###..", "####.", "###..", "##...", "#...."])
    img = np.zeros(cursor.shape + (4,), dtype=np.uint8)
    img[cursor] = (255, 255, 255, 255)
    Image.fromarray(img, "RGBA").save(os.path.join(UI_DIR, "Cursor.png"))
    # A plain white pixel for full-screen fades and rules.
    Image.new("RGBA", (4, 4), (255, 255, 255, 255)).save(os.path.join(UI_DIR, "Pixel.png"))


def callout(text, color, shadow):
    # A word in the pixel font with a coloured drop shadow and a dark outline, readable over any background.
    glyphs = [bitmap(GLYPHS[ch]) for ch in text]
    width = sum(g.shape[1] for g in glyphs) + len(glyphs) - 1
    word = np.zeros((GLYPH_HEIGHT, width), dtype=bool)
    x = 0
    for glyph in glyphs:
        word[:, x:x + glyph.shape[1]] = glyph
        x += glyph.shape[1] + 1
    h, w = word.shape
    front = np.zeros((h + 3, w + 3), dtype=bool)
    front[1:h + 1, 1:w + 1] = word
    drop = np.zeros_like(front)
    drop[2:h + 2, 2:w + 2] = word
    out = np.zeros(front.shape + (4,), dtype=np.uint8)
    out[grow(front | drop)] = rgba("ink")
    out[drop] = rgba(shadow)
    out[front] = rgba(color)
    return Image.fromarray(out, "RGBA")


def tone(path, notes, volume=0.35, rate=22050):
    # Square-wave blips: each note is (frequency, seconds), with a fast decay so they stay short and soft.
    samples = []
    for freq, length in notes:
        n = int(rate * length)
        for i in range(n):
            t = i / rate
            square = 1.0 if (t * freq) % 1.0 < 0.5 else -1.0
            envelope = (1.0 - i / n) ** 2
            samples.append(square * envelope * volume)
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(rate)
        f.writeframes(b"".join(struct.pack("<h", int(s * 32767)) for s in samples))


def sounds():
    tone(os.path.join(AUDIO_DIR, "Move.wav"), [(880, 0.05)], volume=0.2)
    tone(os.path.join(AUDIO_DIR, "Confirm.wav"), [(660, 0.06), (990, 0.1)])
    tone(os.path.join(AUDIO_DIR, "Back.wav"), [(660, 0.06), (440, 0.1)])
    tone(os.path.join(AUDIO_DIR, "Start.wav"), [(523, 0.07), (659, 0.07), (784, 0.07), (1047, 0.25)])
    # A quick bright run upwards for a barrel roll that slipped through danger.
    tone(os.path.join(GAME_AUDIO_DIR, "Dodge.wav"), [(1319, 0.035), (1760, 0.035), (2637, 0.12)], volume=0.22)


def main():
    os.makedirs(UI_DIR, exist_ok=True)
    os.makedirs(AUDIO_DIR, exist_ok=True)
    count = font_atlas()
    logo()
    ui_sprites()
    callout("DODGE!", "cyan", "blue").save(os.path.join(UI_DIR, "Dodge.png"))
    sounds()
    print(f"{count} glyphs, logo, UI frames, dodge callout and 5 sounds")


if __name__ == "__main__":
    main()
