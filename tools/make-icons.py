"""Draws the MultiCalc mark and writes every size the project needs.

The mark is three calculators fanned out behind one another: the back two are
just edges and a screen, the front one carries the keypad. It reads as "more
than one calculator" at 512px and still as "a calculator" at 48px, which is the
only thing a launcher icon has to do.

    python tools/make-icons.py

Writes:
    assets/logo/                                  every size, for stores and readmes
    src/MultiCalc.App/Resources/AppIcon/          what the Android build consumes
    src/MultiCalc.App/Resources/Splash/

Requires Pillow.
"""

from __future__ import annotations

import os
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:
    sys.exit("Pillow is needed: pip install pillow")

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGO_DIR = os.path.join(REPO, "assets", "logo")
APPICON_DIR = os.path.join(REPO, "src", "MultiCalc.App", "Resources", "AppIcon")
SPLASH_DIR = os.path.join(REPO, "src", "MultiCalc.App", "Resources", "Splash")

# Same palette as the app's dark theme, so the icon and the first screen agree.
INK = (11, 13, 16, 255)          # --bg
TEAL = (35, 198, 170, 255)       # --accent
TEAL_DEEP = (15, 110, 96, 255)   # the accent, pushed back
TEAL_DARK = (10, 74, 65, 255)    # pushed back further
ON_TEAL = (4, 34, 29, 255)       # --on-accent
SCREEN = (7, 58, 51, 255)

# Launcher icons get scaled by the system and cropped to a mask, so everything
# meaningful stays inside the middle. 1024 is the working canvas.
CANVAS = 1024

PNG_SIZES = [16, 32, 48, 64, 72, 96, 128, 144, 192, 256, 512, 1024]


def rounded(draw: ImageDraw.ImageDraw, box, radius, fill):
    draw.rounded_rectangle(box, radius=radius, fill=fill)


def draw_mark(size: int, scale: float = 1.0, background: bool = True) -> Image.Image:
    """Draws the mark on a transparent canvas at the given pixel size."""
    # Drawn at 4x and downsampled: Pillow has no antialiasing on shapes, and a
    # rounded rectangle drawn straight at 96px has visibly ragged corners.
    work = size * 4
    image = Image.new("RGBA", (work, work), INK if background else (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    unit = work / CANVAS * scale

    def u(value: float) -> float:
        return value * unit

    body_w, body_h = u(330), u(430)
    radius = u(60)

    # Three bodies, fanned up and to the left, so the stack reads as a count.
    fan = [(-120.0, -94.0, TEAL_DARK), (-60.0, -47.0, TEAL_DEEP), (0.0, 0.0, TEAL)]

    # The fan shifts the mass down and right, so the whole group is pulled back by half
    # its own spread. Without this the mark sits low in the launcher mask.
    cx = work / 2 - u(fan[0][0]) / 2
    cy = work / 2 - u(fan[0][1]) / 2

    offsets = [(u(dx), u(dy), colour) for dx, dy, colour in fan]

    for dx, dy, colour in offsets:
        left = cx - body_w / 2 + dx
        top = cy - body_h / 2 + dy
        rounded(draw, [left, top, left + body_w, top + body_h], radius, colour)

        # A screen on every one, so the back two are still legible as calculators.
        pad = u(34)
        screen_h = u(88)
        rounded(
            draw,
            [left + pad, top + pad, left + body_w - pad, top + pad + screen_h],
            u(18),
            SCREEN if colour is TEAL else ON_TEAL,
        )

    # Keypad, on the front body only.
    left = cx - body_w / 2
    top = cy - body_h / 2
    pad = u(34)
    keys_top = top + pad + u(88) + u(34)
    cols, rows = 3, 3
    gap = u(26)
    key_w = (body_w - pad * 2 - gap * (cols - 1)) / cols
    key_h = u(46)

    for row in range(rows):
        for col in range(cols):
            kx = left + pad + col * (key_w + gap)
            ky = keys_top + row * (key_h + gap)

            # The bottom right key is the equals bar: wider, and the only light one.
            if row == rows - 1 and col == cols - 1:
                continue

            rounded(draw, [kx, ky, kx + key_w, ky + key_h], u(14), ON_TEAL)

    bar_y = keys_top + (rows - 1) * (key_h + gap)
    bar_x = left + pad + 2 * (key_w + gap)
    rounded(draw, [bar_x, bar_y, bar_x + key_w, bar_y + key_h], u(14), SCREEN)

    return image.resize((size, size), Image.LANCZOS)


def write(image: Image.Image, path: str) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path, "PNG")
    print("  " + os.path.relpath(path, REPO))


def main() -> None:
    print("Store and readme sizes:")
    for size in PNG_SIZES:
        write(draw_mark(size), os.path.join(LOGO_DIR, f"icon-{size}.png"))

    # Round variants, for launchers that ask for one.
    for size in (192, 512, 1024):
        mark = draw_mark(size)
        mask = Image.new("L", (size, size), 0)
        ImageDraw.Draw(mask).ellipse([0, 0, size - 1, size - 1], fill=255)
        rounded_icon = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        rounded_icon.paste(mark, (0, 0), mask)
        write(rounded_icon, os.path.join(LOGO_DIR, f"icon-rounded-{size}.png"))

    print("Windows ico:")
    ico_path = os.path.join(LOGO_DIR, "icon.ico")
    draw_mark(256).save(ico_path, sizes=[(s, s) for s in (16, 32, 48, 64, 128, 256)])
    print("  " + os.path.relpath(ico_path, REPO))

    print("What the Android build consumes:")
    # MAUI composites the background file under the foreground file, so the background
    # is a flat colour and only the foreground carries the mark. Putting the mark in
    # both draws it twice, offset.
    background = Image.new("RGBA", (1024, 1024), INK)
    write(background, os.path.join(APPICON_DIR, "appicon.png"))

    # Android crops adaptive icons hard: only the middle ~66% of the foreground is
    # guaranteed to survive the mask, so the mark is drawn small enough to fit that.
    write(draw_mark(1024, scale=0.62, background=False), os.path.join(APPICON_DIR, "appiconfg.png"))
    write(draw_mark(512, scale=0.9, background=False), os.path.join(SPLASH_DIR, "splash.png"))

    print("Done.")


if __name__ == "__main__":
    main()
