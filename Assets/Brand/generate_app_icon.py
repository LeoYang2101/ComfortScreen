from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
ASSETS_DIR = ROOT / "Assets"
BRAND_DIR = ASSETS_DIR / "Brand"

APP_ICON_ICO = ASSETS_DIR / "AppIcon.ico"
APP_ICON_PNG = ASSETS_DIR / "AppIcon.png"
PREVIEW_PNG = BRAND_DIR / "app-icon-preview.png"

ICON_SIZES = [(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)]


def rounded_rect_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def vertical_gradient(size: int, top: tuple[int, int, int], bottom: tuple[int, int, int]) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    pixels = image.load()
    for y in range(size):
        factor = y / max(size - 1, 1)
        row = tuple(int(top[i] * (1 - factor) + bottom[i] * factor) for i in range(3))
        for x in range(size):
            pixels[x, y] = (*row, 255)
    return image


def diagonal_overlay(size: int, start: tuple[int, int, int], end: tuple[int, int, int], opacity: int) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    pixels = image.load()
    for y in range(size):
        for x in range(size):
            factor = (x + y) / max((size - 1) * 2, 1)
            color = tuple(int(start[i] * (1 - factor) + end[i] * factor) for i in range(3))
            pixels[x, y] = (*color, opacity)
    return image


def ellipse_bounds(center_x: float, center_y: float, radius: float) -> tuple[float, float, float, float]:
    return (center_x - radius, center_y - radius, center_x + radius, center_y + radius)


def create_icon_canvas(size: int = 1024) -> Image.Image:
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    radius = int(size * 0.22)
    background = vertical_gradient(size, (57, 200, 191), (45, 107, 234))
    overlay = diagonal_overlay(size, (124, 231, 220), (16, 55, 130), 88)
    background = Image.alpha_composite(background, overlay)

    mask = rounded_rect_mask(size, radius)
    canvas.paste(background, (0, 0), mask)

    highlight = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    highlight_draw = ImageDraw.Draw(highlight)
    highlight_draw.ellipse(
        ellipse_bounds(size * 0.31, size * 0.23, size * 0.28),
        fill=(255, 255, 255, 92),
    )
    highlight = highlight.filter(ImageFilter.GaussianBlur(radius=int(size * 0.025)))
    canvas = Image.alpha_composite(canvas, highlight)

    draw = ImageDraw.Draw(canvas)

    outer_screen = (int(size * 0.18), int(size * 0.205), int(size * 0.82), int(size * 0.655))
    inner_screen = (int(size * 0.205), int(size * 0.228), int(size * 0.795), int(size * 0.635))
    draw.rounded_rectangle(outer_screen, radius=int(size * 0.13), fill=(10, 39, 66, 255))
    draw.rounded_rectangle(
        inner_screen,
        radius=int(size * 0.105),
        fill=(16, 49, 82, 255),
        outline=(242, 252, 255, 232),
        width=max(10, size // 48),
    )

    screen_glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(screen_glow)
    glow_draw.ellipse(
        ellipse_bounds(size * 0.63, size * 0.33, size * 0.28),
        fill=(34, 127, 211, 64),
    )
    screen_glow = screen_glow.filter(ImageFilter.GaussianBlur(radius=int(size * 0.035)))
    canvas = Image.alpha_composite(canvas, screen_glow)

    draw = ImageDraw.Draw(canvas)
    draw.ellipse(ellipse_bounds(size * 0.65, size * 0.338, size * 0.118), fill=(255, 216, 110, 255))
    draw.ellipse(ellipse_bounds(size * 0.62, size * 0.31, size * 0.074), fill=(255, 242, 183, 110))

    wave = [
        (size * 0.305, size * 0.53),
        (size * 0.39, size * 0.425),
        (size * 0.54, size * 0.385),
        (size * 0.675, size * 0.415),
        (size * 0.615, size * 0.535),
        (size * 0.48, size * 0.602),
        (size * 0.325, size * 0.585),
    ]
    draw.polygon(wave, fill=(125, 229, 210, 255))

    wave_reflection = [
        (size * 0.338, size * 0.558),
        (size * 0.418, size * 0.5),
        (size * 0.525, size * 0.482),
        (size * 0.612, size * 0.494),
        (size * 0.56, size * 0.558),
        (size * 0.462, size * 0.595),
        (size * 0.356, size * 0.586),
    ]
    draw.polygon(wave_reflection, fill=(183, 255, 241, 108))

    stand_top = [
        (size * 0.444, size * 0.66),
        (size * 0.556, size * 0.66),
        (size * 0.613, size * 0.718),
        (size * 0.387, size * 0.718),
    ]
    draw.polygon(stand_top, fill=(230, 251, 255, 255))
    draw.rounded_rectangle(
        (int(size * 0.392), int(size * 0.718), int(size * 0.608), int(size * 0.754)),
        radius=int(size * 0.018),
        fill=(213, 246, 243, 255),
    )

    return canvas


def main() -> None:
    BRAND_DIR.mkdir(parents=True, exist_ok=True)
    ASSETS_DIR.mkdir(parents=True, exist_ok=True)

    canvas = create_icon_canvas(1024)
    canvas.save(PREVIEW_PNG)

    app_png = canvas.resize((256, 256), Image.Resampling.LANCZOS)
    app_png.save(APP_ICON_PNG)

    app_png.save(APP_ICON_ICO, sizes=ICON_SIZES)


if __name__ == "__main__":
    main()
