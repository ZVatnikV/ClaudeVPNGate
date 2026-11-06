"""App icon: bone tile, coral lock."""

from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent / "app.ico"
SIZES = [16, 24, 32, 48, 64, 128, 256]


def make(size: int) -> Image.Image:
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    pad = max(1, size // 18)
    draw.rounded_rectangle(
        (pad, pad, size - pad - 1, size - pad - 1),
        radius=max(3, size // 4),
        fill=(243, 240, 230, 255),
    )
    body_w = max(4, int(size * 0.34))
    body_h = max(5, int(size * 0.30))
    left = (size - body_w) // 2
    top = int(size * 0.42)
    draw.rounded_rectangle(
        (left, top, left + body_w, top + body_h),
        radius=max(1, size // 28),
        fill=(255, 93, 58, 255),
    )
    shackle_w = max(3, int(body_w * 0.62))
    shackle_left = (size - shackle_w) // 2
    shackle_top = int(size * 0.26)
    stroke = max(2, size // 16)
    draw.arc(
        (shackle_left, shackle_top, shackle_left + shackle_w, top + stroke),
        start=180,
        end=0,
        fill=(18, 20, 16, 255),
        width=stroke,
    )
    return image


def main() -> None:
    images = [make(size) for size in SIZES]
    images[0].save(OUT, format="ICO", sizes=[(size, size) for size in SIZES], append_images=images[1:])


if __name__ == "__main__":
    main()
