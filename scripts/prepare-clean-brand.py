"""Clean the original large logo without redrawing its lettering or symbol.

Recover opacity from orange colour coverage instead of retaining white matte
pixels. A uniform orange removes the pale fringe; the faint A stays translucent.
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "wwwroot/assets/img"
source = Image.open(ASSETS / "arcteon-logo-tagline.png").convert("RGB")
result = Image.new("RGBA", source.size)
orange = (255, 138, 36)
result.putdata([
    (*orange, round(255 * min(1, max(0, (r - b - 12) / 80))))
    if r > g and r > b + 12 else (0, 0, 0, 0)
    for r, g, b in source.get_flattened_data()
])
result = result.crop(result.getbbox())
output = Image.new("RGBA", (result.width + 8, result.height + 8))
output.paste(result, (4, 4))
output.save(ASSETS / "arcteon-header-footer.png", optimize=True)
print(f"Clean brand: {output.width}x{output.height}")

# Composite for checking edges against the actual dark header colour.
preview = Image.new("RGBA", output.size, "#0b202d")
preview.alpha_composite(output)
preview.thumbnail((1000, 400), Image.Resampling.LANCZOS)
preview.convert("RGB").save(ROOT / "bin/LogoSource/header-footer-clean-check.jpg")
