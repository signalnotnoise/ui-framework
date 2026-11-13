"""Encode the native capture pair with presentation labels. Requires Pillow."""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--captures", type=Path, default=Path("artifacts/skill-preview"))
parser.add_argument("--output", type=Path, default=Path("docs/images/ui-framework-before-after.gif"))
args = parser.parse_args()
font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 20)
small = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 14)
frames = []
for name, label, detail in [
    ("before", "BEFORE / A working screen", "Flat hierarchy / default controls"),
    ("after", "AFTER / A considered workspace", "Clear hierarchy / shared tokens / native controls"),
]:
    with Image.open(args.captures / f"{name}-1040.png") as capture:
        capture = capture.convert("RGB")
    if frames and capture.size != (frames[0].width, frames[0].height - 72):
        raise ValueError("Before/after viewports must match")
    frame = Image.new("RGB", (capture.width, capture.height + 72), "#112D32")
    frame.paste(capture, (0, 72))
    draw = ImageDraw.Draw(frame)
    draw.text((32, 12), label, font=font, fill="#FFFFFF")
    draw.text((32, 42), detail, font=small, fill="#B8D6CC")
    frames.append(frame)
args.output.parent.mkdir(parents=True, exist_ok=True)
frames[0].save(args.output, save_all=True, append_images=frames[1:],
               duration=[2400, 4200], loop=0, disposal=2, optimize=False)
with Image.open(args.output) as gif:
    assert gif.n_frames == 2 and gif.info["loop"] == 0
    for i, duration in enumerate([2400, 4200]):
        gif.seek(i)
        assert gif.info["duration"] == duration
print(f"Verified {args.output}: {frames[0].width} x {frames[0].height}, 2 frames, 6.6 s loop")
