from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
BRAND_DIR = ROOT / "Assets" / "Brand"
OUTPUT_DIR = BRAND_DIR / "FlatCandidates"
ICON_SIZES = [(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)]
PREVIEW_SIZES = [16, 20, 32, 48]

TEAL = (84, 214, 201, 255)
TEAL_SOFT = (156, 239, 226, 255)
BLUE = (36, 93, 199, 255)
BLUE_DARK = (24, 62, 136, 255)
GOLD = (255, 211, 99, 255)
GOLD_SOFT = (255, 238, 173, 255)
WHITE_SOFT = (242, 248, 255, 255)


def new_canvas(size: int = 1024) -> Image.Image:
    return Image.new("RGBA", (size, size), (0, 0, 0, 0))


def blur_ellipse(canvas: Image.Image, bounds: tuple[int, int, int, int], fill: tuple[int, int, int, int], blur: int) -> Image.Image:
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.ellipse(bounds, fill=fill)
    return Image.alpha_composite(canvas, layer.filter(ImageFilter.GaussianBlur(blur)))


def blur_polygon(canvas: Image.Image, points: list[tuple[int, int]], fill: tuple[int, int, int, int], blur: int) -> Image.Image:
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.polygon(points, fill=fill)
    return Image.alpha_composite(canvas, layer.filter(ImageFilter.GaussianBlur(blur)))


def candidate_a(size: int = 1024) -> Image.Image:
    canvas = new_canvas(size)
    canvas = blur_ellipse(canvas, (110, 90, 810, 790), (255, 212, 111, 84), 36)

    draw = ImageDraw.Draw(canvas)
    draw.ellipse((120, 100, 820, 800), fill=GOLD)
    draw.ellipse((420, 70, 1010, 660), fill=BLUE)
    draw.ellipse((275, 255, 635, 615), fill=GOLD_SOFT)
    return canvas


def candidate_b(size: int = 1024) -> Image.Image:
    canvas = new_canvas(size)
    canvas = blur_ellipse(canvas, (390, 360, 635, 605), (255, 214, 120, 120), 28)

    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.arc((215, 150, 860, 795), start=210, end=25, fill=WHITE_SOFT, width=108)
    draw.ellipse((418, 388, 608, 578), fill=GOLD)
    draw.ellipse((468, 438, 558, 528), fill=GOLD_SOFT)
    return Image.alpha_composite(canvas, layer)


def candidate_c(size: int = 1024) -> Image.Image:
    canvas = new_canvas(size)
    canvas = blur_polygon(canvas, [(250, 700), (470, 280), (760, 390), (530, 810)], (120, 224, 209, 70), 20)

    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.polygon([(270, 710), (480, 290), (780, 405), (560, 825)], fill=WHITE_SOFT)
    draw.polygon([(405, 650), (545, 380), (705, 440), (570, 710)], fill=GOLD)
    draw.polygon([(485, 365), (705, 450), (655, 535), (440, 450)], fill=TEAL)
    return Image.alpha_composite(canvas, layer)


def candidate_d(size: int = 1024) -> Image.Image:
    canvas = new_canvas(size)
    canvas = blur_ellipse(canvas, (250, 250, 790, 790), (82, 213, 198, 56), 30)

    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.arc((240, 210, 790, 760), start=222, end=16, fill=TEAL, width=112)
    draw.arc((360, 330, 700, 670), start=222, end=18, fill=BLUE, width=90)
    draw.ellipse((570, 280, 760, 470), fill=GOLD)
    draw.ellipse((612, 322, 710, 420), fill=GOLD_SOFT)
    return Image.alpha_composite(canvas, layer)


def create_size_preview(image: Image.Image, label: str) -> Image.Image:
    tile_width = 112
    tile_height = 98
    preview = Image.new("RGBA", (tile_width * len(PREVIEW_SIZES), tile_height), (255, 255, 255, 255))

    for index, size in enumerate(PREVIEW_SIZES):
        tile = Image.new("RGBA", (tile_width, tile_height), (248, 250, 252, 255))
        dark_half = Image.new("RGBA", (tile_width // 2, tile_height), (33, 38, 45, 255))
        tile.alpha_composite(dark_half, (tile_width // 2, 0))

        scaled = image.resize((size, size), Image.Resampling.LANCZOS)
        left_x = (tile_width // 2 - size) // 2
        right_x = tile_width // 2 + left_x
        y = 14 + (36 - size) // 2

        tile.alpha_composite(scaled, (left_x, y))
        tile.alpha_composite(scaled, (right_x, y))

        draw = ImageDraw.Draw(tile)
        draw.text((8, 6), label, fill=(52, 74, 104, 255))
        draw.text((8, 70), f"{size}x{size}", fill=(43, 47, 53, 255))
        preview.alpha_composite(tile, (index * tile_width, 0))

    return preview


def create_large_preview(image: Image.Image) -> Image.Image:
    card = Image.new("RGBA", (320, 320), (248, 250, 253, 255))
    dark = Image.new("RGBA", (160, 320), (33, 38, 45, 255))
    card.alpha_composite(dark, (160, 0))

    large = image.resize((220, 220), Image.Resampling.LANCZOS)
    card.alpha_composite(large, (50, 50))
    card.alpha_composite(large, (160 + 50, 50))
    return card


def create_comparison_sheet(entries: list[tuple[str, Image.Image, Image.Image]]) -> Image.Image:
    row_height = 400
    width = 1180
    canvas = Image.new("RGBA", (width, row_height * len(entries)), (243, 246, 250, 255))

    for index, (title, large_preview, size_preview) in enumerate(entries):
        top = index * row_height
        card = Image.new("RGBA", (width - 40, row_height - 24), (255, 255, 255, 255))
        draw = ImageDraw.Draw(card)
        draw.rounded_rectangle((0, 0, card.width - 1, card.height - 1), radius=28, fill=(255, 255, 255, 255), outline=(220, 228, 238, 255))
        draw.text((28, 22), title, fill=(31, 41, 55, 255))
        canvas.alpha_composite(card, (20, top + 12))
        canvas.alpha_composite(large_preview, (52, top + 56))
        canvas.alpha_composite(size_preview, (416, top + 138))

    return canvas


def export_candidate(slug: str, label: str, image: Image.Image) -> tuple[Image.Image, Image.Image]:
    png_path = OUTPUT_DIR / f"{slug}.png"
    ico_path = OUTPUT_DIR / f"{slug}.ico"
    preview_path = OUTPUT_DIR / f"{slug}-preview.png"
    sizes_path = OUTPUT_DIR / f"{slug}-sizes.png"

    preview_large = image.resize((1024, 1024), Image.Resampling.LANCZOS)
    preview_large.save(preview_path)

    icon_png = image.resize((256, 256), Image.Resampling.LANCZOS)
    icon_png.save(png_path)
    icon_png.save(ico_path, sizes=ICON_SIZES)

    large_preview = create_large_preview(icon_png)
    size_preview = create_size_preview(icon_png, label)
    large_preview.save(OUTPUT_DIR / f"{slug}-large-card.png")
    size_preview.save(sizes_path)
    return large_preview, size_preview


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    definitions = [
        ("flat-a", "A 月牙", candidate_a()),
        ("flat-b", "B 光点弧线", candidate_b()),
        ("flat-c", "C 斜切光片", candidate_c()),
        ("flat-d", "D 双弧光晕", candidate_d()),
    ]

    comparison_entries: list[tuple[str, Image.Image, Image.Image]] = []
    for slug, label, image in definitions:
        large, sizes = export_candidate(slug, label, image)
        comparison_entries.append((label, large, sizes))

    comparison = create_comparison_sheet(comparison_entries)
    comparison.save(OUTPUT_DIR / "flat-candidate-comparison.png")


if __name__ == "__main__":
    main()
