"""Best-effort enlargement of supplied WorkVibes pixels, without redrawing.

This is a sharpened raster export, not a replacement for an HD/vector original.
"""
from pathlib import Path
from PIL import Image, ImageFilter

root = Path(__file__).resolve().parents[1]
products = root / "wwwroot/assets/img/products"
source = Image.open(products / "product-reference.png").convert("RGB")
# Only the approved clock symbol; omit the embedded WorkVibes caption.
symbol = source.crop((83, 98, 132, 148))
symbol = symbol.resize((490, 500), Image.Resampling.LANCZOS)
symbol = symbol.filter(ImageFilter.UnsharpMask(radius=2.2, percent=150, threshold=4))
output = Image.new("RGBA", (506, 516))
output.paste(symbol, (8, 8))
output.save(products / "workvibes-symbol-hd.png", optimize=True)
print("WorkVibes export: 506x516, transparent outer background, original design preserved")
