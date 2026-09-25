#!/usr/bin/env python3
"""Generate TabuKA logo assets from Assets/temp.jpeg.

Outputs:
  Assets/logo-mark.png          - transparent square mark (in-app)
  Assets/tabuka.ico             - multi-size window icon
  TabuKA.Android/Resources/drawable/icon.png - 512x512 launcher icon
"""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets" / "temp.jpeg"
MARK_PNG = ROOT / "Assets" / "logo-mark.png"
WINDOW_ICO = ROOT / "Assets" / "tabuka.ico"
ANDROID_ICON = ROOT / "TabuKA.Android" / "Resources" / "drawable" / "icon.png"

# Logo facet reds (sampled from source)
BG_CREAM = (251, 248, 243)
ICON_BG = (250, 247, 242)


def is_mark_pixel(r: int, g: int, b: int) -> bool:
    return r > 50 and (r - g) > 35 and (r - b) > 35


def find_red_bbox(im: Image.Image, y0: int = 0, y1: int | None = None) -> tuple[int, int, int, int]:
    w, h = im.size
    if y1 is None:
        y1 = h
    minx, miny, maxx, maxy = w, h, 0, 0
    px = im.load()
    for y in range(y0, min(y1, h), 2):
        for x in range(0, w, 2):
            if is_mark_pixel(*px[x, y][:3]):
                if x < minx:
                    minx = x
                if y < miny:
                    miny = y
                if x > maxx:
                    maxx = x
                if y > maxy:
                    maxy = y
    if maxx <= minx:
        raise RuntimeError(f"No red content found in y={y0}..{y1}")
    return minx, miny, maxx, maxy


def row_has_red(im: Image.Image, y: int) -> bool:
    px = im.load()
    w = im.size[0]
    for x in range(0, w, 3):
        if is_mark_pixel(*px[x, y][:3]):
            return True
    return False


def find_mark_and_text_rows(im: Image.Image) -> tuple[int, int, int, int]:
    """Return (mark_top, mark_bottom, text_top, text_bottom) scan rows."""
    full = find_red_bbox(im)
    # Find vertical gap between mark and text blocks
    y = full[1]
    last_red = full[1]
    gap_start = None
    while y <= full[3]:
        if row_has_red(im, y):
            last_red = y
            gap_start = None
        else:
            if gap_start is None:
                gap_start = y
            elif y - gap_start > 40 and last_red > full[1] + 50:
                break
        y += 2

    mark_bottom = last_red if gap_start is None else gap_start
    text_top = None if gap_start is None else gap_start
    # Resume after gap for text
    if text_top is not None:
        y = text_top
        text_bottom = text_top
        while y <= full[3] + 4:
            if row_has_red(im, y):
                text_bottom = y
            y += 2
        return full[1], mark_bottom, text_top, text_bottom
    return full[1], full[3], full[3], full[3]


def extract_mark(im: Image.Image, pad: int = 24) -> Image.Image:
    mark_top, mark_bottom, _, _ = find_mark_and_text_rows(im)
    bbox = find_red_bbox(im, max(0, mark_top - 2), mark_bottom + 2)
    x0 = max(0, bbox[0] - pad)
    y0 = max(0, bbox[1] - pad)
    x1 = min(im.size[0], bbox[2] + pad)
    y1 = min(im.size[1], bbox[3] + pad)
    crop = im.crop((x0, y0, x1, y1))
    return remove_background(crop)


def remove_background(im: Image.Image) -> Image.Image:
    """Make near-background / light-gray side bars transparent; keep red mark."""
    rgba = im.convert("RGBA")
    px = rgba.load()
    w, h = rgba.size
    # Sample corners for background reference (cream / outer gray)
    corners = [px[0, 0], px[w - 1, 0], px[0, h - 1], px[w - 1, h - 1]]
    # Also sample mid-edge
    samples = corners + [px[w // 2, 0], px[w // 2, h - 1], px[0, h // 2], px[w - 1, h // 2]]

    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            # Keep strong reds and mid reds of the mark
            if is_mark_pixel(r, g, b):
                # Soften jagged light fringe: if clearly pinkish-light edge, partial alpha
                if g > 160 and b > 160:
                    # fringe near white/gray
                    px[x, y] = (r, g, b, 0)
                continue
            # Background-ish: cream, light gray bars, white
            min_dist = min(
                (r - sr) ** 2 + (g - sg) ** 2 + (b - sb) ** 2 for sr, sg, sb, *_ in samples
            )
            # Near neutral light pixels -> transparent
            is_light_neutral = r > 200 and g > 200 and b > 190 and abs(r - g) < 40
            is_near_bg = min_dist < 400
            if is_light_neutral or is_near_bg:
                px[x, y] = (0, 0, 0, 0)
            else:
                # Dark ink edge (dark red outline) keep; mid grays from jpeg noise
                if abs(r - g) < 25 and abs(g - b) < 25 and r < 200:
                    # neutral mid/dark non-red: likely jpeg fringe or bar
                    if min_dist < 2500:
                        px[x, y] = (0, 0, 0, 0)
    return rgba


def square_pad(im: Image.Image, size: int, margin_ratio: float = 0.08, bg=None) -> Image.Image:
    """Fit content into size x size with optional solid background."""
    content = im.copy()
    # Trim transparent borders
    bbox = content.getbbox()
    if bbox:
        content = content.crop(bbox)
    max_content = int(size * (1 - 2 * margin_ratio))
    content.thumbnail((max_content, max_content), Image.Resampling.LANCZOS)
    if bg is None:
        canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    else:
        canvas = Image.new("RGBA", (size, size), bg + (255,))
    ox = (size - content.width) // 2
    oy = (size - content.height) // 2
    canvas.alpha_composite(content, (ox, oy))
    return canvas


def save_ico(im: Image.Image, path: Path) -> None:
    sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    im.save(path, format="ICO", sizes=sizes)


def main() -> int:
    if not SOURCE.exists():
        print(f"Missing source: {SOURCE}", file=sys.stderr)
        return 1

    im = Image.open(SOURCE).convert("RGB")
    mark = extract_mark(im)

    # In-app transparent mark at 512
    mark_512 = square_pad(mark, 512, margin_ratio=0.04, bg=None)
    MARK_PNG.parent.mkdir(parents=True, exist_ok=True)
    mark_512.save(MARK_PNG, "PNG", optimize=True)
    print(f"Wrote {MARK_PNG.relative_to(ROOT)} {mark_512.size}")

    # Window icon: cream rounded-ish square background matching brand
    icon_src = square_pad(mark, 256, margin_ratio=0.12, bg=ICON_BG)
    save_ico(icon_src, WINDOW_ICO)
    print(f"Wrote {WINDOW_ICO.relative_to(ROOT)}")

    # Android launcher icon 512
    android = square_pad(mark, 512, margin_ratio=0.14, bg=ICON_BG)
    ANDROID_ICON.parent.mkdir(parents=True, exist_ok=True)
    android.convert("RGB").save(ANDROID_ICON, "PNG", optimize=True)
    print(f"Wrote {ANDROID_ICON.relative_to(ROOT)} {android.size}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
