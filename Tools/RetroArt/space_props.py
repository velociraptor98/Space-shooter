#!/usr/bin/env python3
"""Generates the background scenery (nebulae, galaxies, planets) and the asteroid hazards.

Everything is procedural - layered noise, simple sphere lighting - quantised to the shared Sweetie 16
palette with ordered dithering, so each piece is unique but sits in the same 16-bit style. Seeds are
fixed, so re-running produces the same art.

Usage: python3 Tools/RetroArt/space_props.py
"""
import math
import os

import numpy as np
from PIL import Image

from retro_sprites import PALETTE, ROOT

SPACE_DIR = os.path.join(ROOT, "Assets/Sprites/Space")
ASTEROID_DIR = os.path.join(ROOT, "Assets/Sprites/Asteroids")
LIGHT = np.array([-0.55, 0.55, 0.63])  # From the upper left, towards the viewer.
BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 - 0.5


def fbm(width, height, rng, base=4, octaves=5):
    # Fractal value noise in 0..1: random grids at doubling resolutions, smoothly upscaled and summed.
    total = np.zeros((height, width))
    amplitude, weight = 1.0, 0.0
    for octave in range(octaves):
        cells = base * 2 ** octave
        grid = rng.random((cells + 1, cells + 1))
        layer = np.asarray(Image.fromarray((grid * 255).astype(np.uint8)).resize((width, height), Image.Resampling.BICUBIC)) / 255.0
        total += layer * amplitude
        weight += amplitude
        amplitude *= 0.5
    total /= weight
    return (total - total.min()) / max(total.max() - total.min(), 1e-6)


def dithered_levels(value, levels):
    # Quantise 0..1 values to 0..levels-1 with a 4x4 ordered dither, for 16-bit style gradients.
    h, w = value.shape
    threshold = np.tile(BAYER4, (h // 4 + 1, w // 4 + 1))[:h, :w]
    return np.clip(np.floor(value * levels + threshold), 0, levels - 1).astype(int)


def colorize(levels, ramp, alpha):
    colors = np.array([PALETTE[c] for c in ramp], dtype=np.uint8)
    out = np.zeros(levels.shape + (4,), dtype=np.uint8)
    out[..., :3] = colors[levels]
    out[..., 3] = np.where(alpha, 255, 0)
    return Image.fromarray(out, "RGBA")


def nebula(width, height, ramp, seed):
    # A soft cloud: noise shaped by an elliptical falloff, fading to transparent through a dither.
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:height, 0:width]
    d = np.sqrt(((x - width / 2) / (width / 2)) ** 2 + ((y - height / 2) / (height / 2)) ** 2)
    falloff = np.clip(1.0 - d, 0.0, 1.0) ** 0.7
    density = fbm(width, height, rng, base=3) * falloff
    density = density / max(density.max(), 1e-6)
    # Level 0 is empty space; the rest step through the ramp from faint to dense.
    levels = dithered_levels(density, len(ramp) + 1)
    return colorize(np.maximum(levels - 1, 0), ramp, levels > 0)


def galaxy(size, ramp, seed, tilt=0.55):
    # A two-armed spiral seen at an angle, with a bright core.
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:size, 0:size]
    u, v = (x - size / 2) / (size / 2), (y - size / 2) / (size / 2) / tilt
    r = np.sqrt(u ** 2 + v ** 2) + 1e-6
    theta = np.arctan2(v, u)
    arms = 0.5 + 0.5 * np.cos(2.0 * (theta - 2.6 * np.log(r)))
    noise = fbm(size, size, rng, base=6)
    brightness = (arms * 0.6 + noise * 0.4) * np.clip(1.0 - r, 0.0, 1.0) ** 1.2 + np.clip(0.35 - r, 0.0, 1.0) * 2.5
    brightness = np.clip(brightness / max(brightness.max(), 1e-6), 0.0, 1.0)
    levels = dithered_levels(brightness, len(ramp) + 1)
    return colorize(np.maximum(levels - 1, 0), ramp, levels > 0)


def sphere(diameter):
    radius = diameter / 2.0
    y, x = np.mgrid[0:diameter, 0:diameter]
    nx, ny = (x + 0.5 - radius) / radius, -(y + 0.5 - radius) / radius
    inside = nx ** 2 + ny ** 2 <= 1.0
    nz = np.sqrt(np.clip(1.0 - nx ** 2 - ny ** 2, 0.0, 1.0))
    shade = np.clip(nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2], 0.0, 1.0)
    return inside, shade, nx, ny


def planet(diameter, ramp, kind, seed, ring=None):
    rng = np.random.default_rng(seed)
    inside, shade, nx, ny = sphere(diameter)
    noise = fbm(diameter, diameter, rng, base=3)
    if kind == "bands":
        surface = 0.5 + 0.5 * np.sin(ny * 9.0 + noise * 4.0)
    elif kind == "lava":
        surface = 1.0 - np.abs(noise - 0.5) * 4.0
        surface = np.clip(surface, 0.0, 1.0) ** 3
    else:
        surface = noise
    value = np.clip(shade * 0.7 + surface * 0.3, 0.0, 1.0) * (shade > 0.02)
    image = colorize(dithered_levels(value, len(ramp)), ramp, inside)
    if ring:
        image = add_ring(image, diameter, ring)
    return image


def add_ring(planet_image, diameter, ramp):
    # A tilted ring: drawn behind the planet across its top half and in front across its bottom half.
    pad = diameter // 2
    size = (diameter + pad * 2, diameter + pad)
    canvas = np.zeros((size[1], size[0], 4), dtype=np.uint8)
    y, x = np.mgrid[0:size[1], 0:size[0]]
    cx, cy = size[0] / 2, size[1] / 2
    d = np.sqrt(((x - cx) / (diameter * 0.95)) ** 2 + ((y - cy) / (diameter * 0.22)) ** 2)
    ring = (d > 0.72) & (d < 1.0)
    ring_colors = np.array([PALETTE[c] for c in ramp], dtype=np.uint8)
    band = np.clip(((d - 0.72) / 0.28 * len(ramp)).astype(int), 0, len(ramp) - 1)
    behind = ring & (y < cy)
    canvas[behind, :3] = ring_colors[band[behind]]
    canvas[behind, 3] = 255
    planet_pixels = np.asarray(planet_image)
    ox, oy = pad, (size[1] - diameter) // 2
    region = canvas[oy:oy + diameter, ox:ox + diameter]
    solid = planet_pixels[..., 3] > 0
    region[solid] = planet_pixels[solid]
    front = ring & (y >= cy)
    canvas[front, :3] = ring_colors[band[front]]
    canvas[front, 3] = 255
    return Image.fromarray(canvas, "RGBA")


def asteroid(radius, seed):
    # A lumpy rock: a wobbling outline, lit like a sphere, pocked with craters, outlined in ink.
    rng = np.random.default_rng(seed)
    size = radius * 2 + 4
    y, x = np.mgrid[0:size, 0:size]
    dx, dy = x + 0.5 - size / 2, -(y + 0.5 - size / 2)
    angle = np.arctan2(dy, dx)
    wobble = sum(rng.uniform(0.05, 0.12) * np.sin(k * angle + rng.uniform(0, 2 * math.pi)) for k in (2, 3, 5))
    edge = radius * (1.0 + wobble)
    dist = np.sqrt(dx ** 2 + dy ** 2)
    inside = dist <= edge
    nx, ny = dx / edge, dy / edge
    nz = np.sqrt(np.clip(1.0 - nx ** 2 - ny ** 2, 0.0, 1.0))
    shade = np.clip(nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2], 0.0, 1.0)
    for _ in range(max(2, radius // 4)):
        cr = rng.uniform(radius * 0.12, radius * 0.25)
        ca = rng.uniform(0, 2 * math.pi)
        cd = rng.uniform(0, radius * 0.6)
        crater = np.sqrt((dx - cd * math.cos(ca)) ** 2 + (dy - cd * math.sin(ca)) ** 2) < cr
        shade = np.where(crater, shade * 0.55, shade)
    # Hard shading bands rather than dither: hazards should read as solid shapes at a glance.
    ramp = ["ink", "steel", "slate", "silver"]
    levels = np.clip(np.floor(shade ** 1.2 * len(ramp) + 0.25), 0, len(ramp) - 1).astype(int)
    image = np.asarray(colorize(levels, ramp, inside)).copy()
    solid = image[..., 3] > 0
    grown = solid.copy()
    grown[1:] |= solid[:-1]
    grown[:-1] |= solid[1:]
    grown[:, 1:] |= solid[:, :-1]
    grown[:, :-1] |= solid[:, 1:]
    image[grown & ~solid] = (*PALETTE["ink"], 255)
    return Image.fromarray(image, "RGBA")


def main():
    os.makedirs(SPACE_DIR, exist_ok=True)
    os.makedirs(ASTEROID_DIR, exist_ok=True)
    # Nebulae stay dim so they never compete with bullets for attention.
    nebulae = [
        (170, 110, ["navy", "blue", "sky"]),
        (140, 140, ["plum", "red", "orange"]),
        (190, 100, ["navy", "teal", "cyan"]),
        # Darker than the asteroids, so rocks keep their contrast when they cross it.
        (120, 150, ["navy", "steel", "slate"]),
        (160, 120, ["navy", "plum", "blue"]),
        (130, 100, ["plum", "blue", "sky"]),
    ]
    for i, (w, h, ramp) in enumerate(nebulae):
        nebula(w, h, ramp, seed=100 + i).save(os.path.join(SPACE_DIR, f"Nebula_{i}.png"))
    galaxies = [(72, ["navy", "blue", "sky", "white"]), (56, ["plum", "red", "orange", "yellow"]), (64, ["navy", "teal", "cyan", "white"])]
    for i, (size, ramp) in enumerate(galaxies):
        galaxy(size, ramp, seed=200 + i).save(os.path.join(SPACE_DIR, f"Galaxy_{i}.png"))
    planets = [
        (56, ["ink", "plum", "red", "orange", "yellow"], "bands", ["steel", "slate", "silver"]),
        (44, ["ink", "navy", "blue", "sky", "cyan"], "bands", None),
        (24, ["ink", "steel", "slate", "silver"], "rock", None),
        (32, ["ink", "navy", "sky", "cyan", "white"], "rock", None),
        (28, ["ink", "plum", "red", "orange", "yellow"], "lava", None),
        (38, ["ink", "navy", "teal", "green", "lime"], "rock", ["navy", "blue"]),
    ]
    for i, (diameter, ramp, kind, ring) in enumerate(planets):
        planet(diameter, ramp, kind, seed=300 + i, ring=ring).save(os.path.join(SPACE_DIR, f"Planet_{i}.png"))
    for size, radius in (("Large", 18), ("Medium", 11), ("Small", 6)):
        for i in range(3):
            asteroid(radius, seed=400 + radius * 10 + i).save(os.path.join(ASTEROID_DIR, f"{size}_{i}.png"))
    print(f"{len(nebulae)} nebulae, {len(galaxies)} galaxies, {len(planets)} planets, 9 asteroids")


if __name__ == "__main__":
    main()
