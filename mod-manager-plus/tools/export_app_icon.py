"""Собрать multi-size .ico (PNG entries) для Windows / Avalonia."""
from __future__ import annotations

import struct
from io import BytesIO
from pathlib import Path

from PIL import Image

SRC = Path(r"C:\Users\admin\.cursor\projects\c-projects-TNI-ModManager-Plus\assets\app-icon-c-final.png")
ASSETS = Path(__file__).resolve().parents[1] / "src" / "TniModManager" / "Assets"
SIZES = [16, 24, 32, 48, 64, 128, 256]


def png_bytes(img: Image.Image, size: int) -> bytes:
    buf = BytesIO()
    img.resize((size, size), Image.Resampling.LANCZOS).save(buf, format="PNG")
    return buf.getvalue()


def write_ico(path: Path, images: list[tuple[int, bytes]]) -> None:
    # ICONDIR + ICONDIRENTRY * n + data
    count = len(images)
    offset = 6 + 16 * count
    entries = []
    blobs = []
    for size, data in images:
        w = 0 if size >= 256 else size
        h = 0 if size >= 256 else size
        entries.append(struct.pack(
            "<BBBBHHII",
            w, h, 0, 0,
            1, 32,
            len(data), offset,
        ))
        blobs.append(data)
        offset += len(data)

    with path.open("wb") as f:
        f.write(struct.pack("<HHH", 0, 1, count))
        for e in entries:
            f.write(e)
        for b in blobs:
            f.write(b)


def main() -> None:
    img = Image.open(SRC).convert("RGBA")
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a >= 8 and r > 245 and g > 245 and b > 245:
                px[x, y] = (0, 0, 0, 0)

    icons = ASSETS / "Icons"
    icons.mkdir(parents=True, exist_ok=True)
    img.resize((256, 256), Image.Resampling.LANCZOS).save(icons / "app-icon.png", "PNG")
    img.resize((512, 512), Image.Resampling.LANCZOS).save(ASSETS / "app-icon.png", "PNG")

    packed = [(s, png_bytes(img, s)) for s in SIZES]
    ico = ASSETS / "app.ico"
    write_ico(ico, packed)
    print(f"ico={ico} bytes={ico.stat().st_size} sizes={SIZES}")


if __name__ == "__main__":
    main()
