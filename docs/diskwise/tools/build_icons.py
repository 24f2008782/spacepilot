#!/usr/bin/env python3
"""Build the DiskWise icon asset set from the SVG masters.

Outputs:
  assets/icons/png/diskwise-<size>.png      master (dark tile) exports
  assets/icons/png/dark-<size>.png          transparent / dark-theme exports
  assets/icons/png/light-<size>.png         light-theme exports
  assets/icons/diskwise.ico                 multi-resolution Windows icon
  assets/icons/png/preview-sizes.png        size ramp on light + dark backgrounds
  assets/icons/png/preview-lockup.png       icon + wordmark lockup
"""
import io
import os
import subprocess

import cairosvg
from PIL import Image, ImageDraw, ImageFont

SVG = "assets/icons/svg"
PNG = "assets/icons/png"

ZIE = {
    "master": [256, 128, 96, 64, 48, 40, 32],
    "small": [24, 20, 16],
}
THEMES = {
    "master": "diskwise_master.svg",
    "dark": "diskwise_dark.svg",
    "light": "diskwise_light.svg",
}


def render(svg_name: str, size: int) -> Image.Image:
    data = cairosvg.svg2png(
        url=os.path.join(SVG, svg_name), output_width=size, output_height=size
    )
    return Image.open(io.BytesIO(data)).convert("RGBA")


def main() -> None:
    os.makedirs(PNG, exist_ok=True)
    ico_frames = []

    for theme, fname in THEMES.items():
        for sizes_key, sizes in ZIE.items():
            for s in sizes:
                im = render(fname, s)
                prefix = "" if theme == "master" else f"{theme}-"
                im.save(os.path.join(PNG, f"{prefix}{s:03d}.png"))
                if theme == "master":
                    ico_frames.append((s, im))

    # Large masters for store/store-listing use.
    for s in (512, 1024):
        render(THEMES["master"], s).save(os.path.join(PNG, f"master-{s}.png"))

    # --- multi-resolution ICO -------------------------------------------------
    # Use the optical small-size variant for 16-24 px and the detailed art above.
    small = {s: render("diskwise_small.svg", s) for s in (16, 20, 24)}
    frames = [small[s] for s in (16, 20, 24)] + [im for s, im in sorted(ico_frames)]
    tmp_paths = []
    for i, im in enumerate(frames):
        p = f"/tmp/ico_{i:02d}_{im.width}.png"
        im.save(p)
        tmp_paths.append(p)

    ico_path = "assets/icons/diskwise.ico"
    subprocess.run(["magick", *tmp_paths, ico_path], check=True)

    # --- previews -------------------------------------------------------------
    bg_light, bg_dark = (243, 246, 250), (13, 19, 31)
    ramp = [256, 64, 48, 32, 24, 20, 16]
    pad, gap = 28, 22
    width = pad * 2 + sum(ramp) + gap * (len(ramp) - 1)
    height = 256 + pad * 2
    sheet = Image.new("RGBA", (width, height * 2), bg_light)
    ImageDraw.Draw(sheet).rectangle([0, height, width, height * 2], fill=bg_dark)
    for row in range(2):
        x = pad
        for s in ramp:
            im = render(THEMES["master"], s) if s >= 32 else render("diskwise_small.svg", s)
            y = row * height + pad + (256 - s) // 2
            sheet.alpha_composite(im, (x, y))
            x += s + gap
    sheet.convert("RGB").save(os.path.join(PNG, "preview-sizes.png"))

    # lockup
    try:
        bold = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 96)
        reg = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 34)
    except OSError:
        bold = reg = ImageFont.load_default()

    lock = Image.new("RGBA", (1100, 320), bg_dark)
    icon = render(THEMES["master"], 224)
    lock.alpha_composite(icon, (40, 48))
    d = ImageDraw.Draw(lock)
    d.text((300, 86), "DiskWise", font=bold, fill=(232, 241, 255))
    d.text((302, 196), "Safe cleanup  ·  Disk intelligence  ·  Application control",
           font=reg, fill=(120, 190, 210))
    lock.convert("RGB").save(os.path.join(PNG, "preview-lockup.png"))

    # --- verify ICO -----------------------------------------------------------
    with Image.open(ico_path) as ico:
        print("ICO sizes:", sorted(ico.info.get("sizes", [])))
    print("frames written:", len(frames))
    print("png files:", len(os.listdir(PNG)))


if __name__ == "__main__":
    main()
