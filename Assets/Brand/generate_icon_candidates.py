from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
BRAND_DIR = ROOT / "Assets" / "Brand"
CANDIDATES_DIR = BRAND_DIR / "Candidates"
ICON_SIZES = [(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)]
PREVIEW_SIZES = [16, 20, 32, 48]


def rounded_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def vertical_gradient(size: int, top: tuple[int, int, int], bottom: tuple[int, int, int]) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    pixels = image.load()
    for y in range(size):
        ratio = y / max(size - 1, 1)
        row = tuple(int(top[index] * (1 - ratio) + bottom[index] * ratio) for index in range(3))
        for x in range(size):
            pixels[x, y] = (*row, 255)
    return image


def diagonal_tint(size: int, start: tuple[int, int, int], end: tuple[int, int, int], alpha: int) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    pixels = image.load()
    for y in range(size):
        for x in range(size):
            ratio = (x + y) / max((size - 1) * 2, 1)
            color = tuple(int(start[index] * (1 - ratio) + end[index] * ratio) for index in range(3))
            pixels[x, y] = (*color, alpha)
    return image


def ellipse(bounds: tuple[float, float, float, float], fill: tuple[int, int, int, int], blur: int = 0) -> Image.Image:
    left, top, right, bottom = bounds
    size = int(max(right, bottom, 1024))
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.ellipse(bounds, fill=fill)
    return image.filter(ImageFilter.GaussianBlur(blur)) if blur else image


def build_base(size: int = 1024) -> Image.Image:
    image = vertical_gradient(size, (70, 209, 196), (52, 102, 214))
    image = Image.alpha_composite(image, diagonal_tint(size, (165, 240, 232), (15, 54, 128), 64))

    highlight = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    highlight_draw = ImageDraw.Draw(highlight)
    highlight_draw.ellipse((88, 78, 470, 410), fill=(255, 255, 255, 86))
    highlight = highlight.filter(ImageFilter.GaussianBlur(30))

    result = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    mask = rounded_mask(size, int(size * 0.23))
    result.paste(image, (0, 0), mask)
    result = Image.alpha_composite(result, highlight)
    return result


def create_candidate_a(size: int = 1024) -> Image.Image:
    canvas = build_base(size)

    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.ellipse((365, 220, 820, 675), fill=(255, 225, 119, 120))
    glow = glow.filter(ImageFilter.GaussianBlur(48))
    canvas = Image.alpha_composite(canvas, glow)

    draw = ImageDraw.Draw(canvas)
    draw.ellipse((360, 210, 815, 665), fill=(255, 221, 118, 255))
    draw.ellipse((510, 200, 910, 600), fill=(34, 102, 184, 255))

    inner = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    inner_draw = ImageDraw.Draw(inner)
    inner_draw.ellipse((440, 255, 760, 575), fill=(255, 246, 198, 82))
    inner = inner.filter(ImageFilter.GaussianBlur(6))
    return Image.alpha_composite(canvas, inner)


def create_candidate_b(size: int = 1024) -> Image.Image:
    canvas = build_base(size)

    orbit = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    orbit_draw = ImageDraw.Draw(orbit)
    orbit_draw.arc((210, 180, 835, 805), start=212, end=32, fill=(237, 250, 255, 255), width=96)
    orbit = orbit.filter(ImageFilter.GaussianBlur(2))
    canvas = Image.alpha_composite(canvas, orbit)

    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.ellipse((404, 370, 620, 586), fill=(255, 223, 117, 150))
    glow = glow.filter(ImageFilter.GaussianBlur(30))
    canvas = Image.alpha_composite(canvas, glow)

    draw = ImageDraw.Draw(canvas)
    draw.ellipse((420, 386, 604, 570), fill=(255, 222, 116, 255))
    draw.ellipse((455, 422, 569, 536), fill=(255, 246, 201, 150))
    return canvas


def create_candidate_c(size: int = 1024) -> Image.Image:
    canvas = build_base(size)

    shape = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    shape_draw = ImageDraw.Draw(shape)
    shape_draw.polygon(
        [
            (290, 626),
            (472, 280),
            (760, 380),
            (572, 736),
        ],
        fill=(226, 251, 255, 255),
    )
    shape = shape.filter(ImageFilter.GaussianBlur(2))
    canvas = Image.alpha_composite(canvas, shape)

    accent = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    accent_draw = ImageDraw.Draw(accent)
    accent_draw.polygon(
        [
            (360, 650),
            (510, 390),
            (694, 456),
            (552, 700),
        ],
        fill=(255, 220, 116, 255),
    )
    accent = accent.filter(ImageFilter.GaussianBlur(1))
    canvas = Image.alpha_composite(canvas, accent)

    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.ellipse((470, 275, 750, 555), fill=(255, 231, 154, 88))
    glow = glow.filter(ImageFilter.GaussianBlur(45))
    return Image.alpha_composite(canvas, glow)


def create_size_preview(image: Image.Image, label: str) -> Image.Image:
    card_width = 96
    card_height = 96
    preview = Image.new("RGBA", (card_width * len(PREVIEW_SIZES), card_height), (255, 255, 255, 255))

    for index, size in enumerate(PREVIEW_SIZES):
        tile = Image.new("RGBA", (card_width, card_height), (245, 247, 250, 255))
        dark_half = Image.new("RGBA", (card_width // 2, card_height), (34, 38, 44, 255))
        tile.alpha_composite(dark_half, (card_width // 2, 0))

        scaled = image.resize((size, size), Image.Resampling.LANCZOS)
        left_x = (card_width // 2 - size) // 2
        right_x = card_width // 2 + left_x
        y = 12 + (36 - size) // 2

        tile.alpha_composite(scaled, (left_x, y))
        tile.alpha_composite(scaled, (right_x, y))

        draw = ImageDraw.Draw(tile)
        draw.text((8, 66), f"{size}x{size}", fill=(45, 49, 55, 255))
        draw.text((8, 6), label, fill=(59, 78, 108, 255))
        preview.alpha_composite(tile, (index * card_width, 0))

    return preview


def create_comparison_sheet(entries: list[tuple[str, Image.Image, Image.Image]]) -> Image.Image:
    row_height = 380
    width = 1120
    canvas = Image.new("RGBA", (width, row_height * len(entries)), (244, 247, 251, 255))
    draw = ImageDraw.Draw(canvas)

    for index, (title, large, sizes) in enumerate(entries):
        top = index * row_height
        card = Image.new("RGBA", (width - 48, row_height - 28), (255, 255, 255, 255))
        card_draw = ImageDraw.Draw(card)
        card_draw.rounded_rectangle((0, 0, card.width - 1, card.height - 1), radius=28, fill=(255, 255, 255, 255), outline=(219, 229, 240, 255))
        canvas.alpha_composite(card, (24, top + 14))

        draw.text((56, top + 36), title, fill=(31, 41, 55, 255))

        large_card = Image.new("RGBA", (288, 288), (248, 250, 253, 255))
        large_card.alpha_composite(large.resize((256, 256), Image.Resampling.LANCZOS), (16, 16))
        canvas.alpha_composite(large_card, (56, top + 64))
        canvas.alpha_composite(sizes, (390, top + 118))

    return canvas


def export_candidate(slug: str, label: str, image: Image.Image) -> tuple[Image.Image, Image.Image]:
    png_path = CANDIDATES_DIR / f"{slug}.png"
    ico_path = CANDIDATES_DIR / f"{slug}.ico"
    preview_path = CANDIDATES_DIR / f"{slug}-preview.png"
    sizes_path = CANDIDATES_DIR / f"{slug}-sizes.png"

    preview_large = image.resize((1024, 1024), Image.Resampling.LANCZOS)
    preview_large.save(preview_path)

    png = image.resize((256, 256), Image.Resampling.LANCZOS)
    png.save(png_path)
    png.save(ico_path, sizes=ICON_SIZES)

    size_preview = create_size_preview(png, label)
    size_preview.save(sizes_path)

    return preview_large, size_preview


def main() -> None:
    CANDIDATES_DIR.mkdir(parents=True, exist_ok=True)

    definitions = [
        ("candidate-a", "A 月牙光晕", create_candidate_a()),
        ("candidate-b", "B 光点保护弧", create_candidate_b()),
        ("candidate-c", "C 非对称光斑", create_candidate_c()),
    ]

    comparison_entries: list[tuple[str, Image.Image, Image.Image]] = []
    for slug, label, image in definitions:
        large, sizes = export_candidate(slug, label, image)
        comparison_entries.append((label, large, sizes))

    comparison = create_comparison_sheet(comparison_entries)
    comparison.save(CANDIDATES_DIR / "candidate-comparison.png")


if __name__ == "__main__":
    main()
