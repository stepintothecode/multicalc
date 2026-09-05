"""Turns raw device screenshots into the exact assets Google Play asks for.

Play wants phone screenshots at a 16:9 or 9:16 ratio, and a phone screenshot is
none of those: a Galaxy A55 shot is 1080x2340, which is 9:19.5. Scaling one to
fit would squash it, so each is cropped free of the status and navigation bars,
then placed on a 1080x1920 canvas with a caption above it.

    python tools/make-store-assets.py <folder-of-raw-screenshots>

Writes into assets/store/:
    screenshots/  1080x1920 phone screenshots, in listing order
    icon-512.png  the Play Store icon
    feature-1024x500.png  the feature graphic

Requires Pillow.
"""

from __future__ import annotations

import os
import sys

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    sys.exit("Pillow is needed: pip install pillow")

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STORE = os.path.join(REPO, "assets", "store")
LOGO = os.path.join(REPO, "assets", "logo")

# The app's own dark palette, so the surround matches the product.
INK = (11, 13, 16, 255)
TEAL = (35, 198, 170, 255)
TEXT = (236, 239, 243, 255)
DIM = (139, 149, 161, 255)

SHOT_W, SHOT_H = 1080, 1920
FEATURE_W, FEATURE_H = 1024, 500

# What to crop off a raw 2340 tall capture: the status bar at the top and the
# gesture or button bar at the bottom. Both are the phone's, not the app's.
STATUS_BAR = 92
NAV_BAR = 132

# Caption, then the shot. Order here is the order Play shows them in.
SCREENS = [
    ("01-calculator", "Every calculator keeps its own display"),
    ("02-calculators", "Name them, colour them, switch in a tap"),
    ("03-colours", "Any colour you like, not a fixed set"),
    ("04-history", "A separate history tape for each one"),
    ("05-settings", "Export and import your history"),
    ("06-about", "Two kinds of calculator, explained"),
]


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = (
        ["C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/arialbd.ttf"]
        if bold
        else ["C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf"]
    )

    for path in candidates:
        if os.path.exists(path):
            return ImageFont.truetype(path, size)

    return ImageFont.load_default()


def rounded(image: Image.Image, radius: int) -> Image.Image:
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, image.width - 1, image.height - 1], radius, fill=255)
    out = Image.new("RGBA", image.size, (0, 0, 0, 0))
    out.paste(image, (0, 0), mask)
    return out


def build_screenshot(raw_path: str, caption: str) -> Image.Image:
    raw = Image.open(raw_path).convert("RGBA")
    body = raw.crop((0, STATUS_BAR, raw.width, raw.height - NAV_BAR))

    canvas = Image.new("RGBA", (SHOT_W, SHOT_H), INK)
    draw = ImageDraw.Draw(canvas)

    caption_font = font(52, bold=True)
    caption_top = 78

    # Two lines if it does not fit on one, wrapped on whole words.
    words = caption.split()
    lines, current = [], ""
    for word in words:
        trial = (current + " " + word).strip()
        if draw.textlength(trial, font=caption_font) <= SHOT_W - 140:
            current = trial
        else:
            lines.append(current)
            current = word
    lines.append(current)

    y = caption_top
    for line in lines:
        width = draw.textlength(line, font=caption_font)
        draw.text(((SHOT_W - width) / 2, y), line, font=caption_font, fill=TEXT)
        y += 64

    # The shot fills whatever is left, keeping its aspect ratio.
    top = y + 46
    available_h = SHOT_H - top - 60
    scale = min((SHOT_W - 150) / body.width, available_h / body.height)
    shot = body.resize((int(body.width * scale), int(body.height * scale)), Image.LANCZOS)
    shot = rounded(shot, 34)

    canvas.paste(shot, ((SHOT_W - shot.width) // 2, top), shot)

    return canvas.convert("RGB")


def build_feature() -> Image.Image:
    canvas = Image.new("RGBA", (FEATURE_W, FEATURE_H), INK)
    draw = ImageDraw.Draw(canvas)

    mark_path = os.path.join(LOGO, "icon-512.png")
    if os.path.exists(mark_path):
        mark = Image.open(mark_path).convert("RGBA").resize((300, 300), Image.LANCZOS)
        canvas.paste(mark, (70, (FEATURE_H - 300) // 2), mark)

    draw.text((430, 175), "MultiCalc", font=font(76, bold=True), fill=TEXT)
    draw.text((432, 268), "More than one calculator,", font=font(34), fill=DIM)
    draw.text((432, 312), "at the same time.", font=font(34), fill=TEAL)

    return canvas.convert("RGB")


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit("Give me the folder holding the raw device screenshots.")

    raw_dir = sys.argv[1]
    shots_dir = os.path.join(STORE, "screenshots")
    os.makedirs(shots_dir, exist_ok=True)

    print("Phone screenshots (1080x1920):")
    made = 0
    for index, (name, caption) in enumerate(SCREENS, start=1):
        raw_path = os.path.join(raw_dir, name + ".png")

        if not os.path.exists(raw_path):
            print(f"  missing {name}.png, skipped")
            continue

        out = os.path.join(shots_dir, f"{index:02d}-{name.split('-', 1)[1]}.png")
        build_screenshot(raw_path, caption).save(out, "PNG")
        print("  " + os.path.relpath(out, REPO))
        made += 1

    print("Store icon (512x512):")
    icon_src = os.path.join(LOGO, "icon-512.png")
    if os.path.exists(icon_src):
        icon_out = os.path.join(STORE, "icon-512.png")
        Image.open(icon_src).convert("RGB").save(icon_out, "PNG")
        print("  " + os.path.relpath(icon_out, REPO))

    print("Feature graphic (1024x500):")
    feature_out = os.path.join(STORE, "feature-1024x500.png")
    build_feature().save(feature_out, "PNG")
    print("  " + os.path.relpath(feature_out, REPO))

    print(f"Done. {made} screenshots.")


if __name__ == "__main__":
    main()
