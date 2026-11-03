"""Prepare web previews after export-sep-presentation.ps1; preserve slide layout."""
from pathlib import Path
from PIL import Image
from zipfile import ZipFile
import json
import re
import xml.etree.ElementTree as ET

repo = Path(__file__).resolve().parents[1]
public = repo / "wwwroot/assets/presentations/sep"
slides = sorted((repo / "bin/SEP-presentation").glob("slide-*.png"))
with ZipFile(public / "arcteon-digital-sep.pptx") as deck:
    slide_parts = [name for name in deck.namelist() if re.fullmatch(r"ppt/slides/slide\d+\.xml", name)]
    assert len(slide_parts) == len(slides), "Export every slide before preparing web images"
    metadata = []
    for index, path in enumerate(slides, 1):
        image = Image.open(path).convert("RGB")
        image.save(public / f"{path.stem}.webp", quality=90, method=6)
        image.resize((240, 135), Image.Resampling.LANCZOS).save(public / f"{path.stem}-thumb.webp", quality=85)
        texts = ET.fromstring(deck.read(f"ppt/slides/slide{index}.xml")).findall(".//{http://schemas.openxmlformats.org/drawingml/2006/main}t")
        title = " ".join(item.text or "" for item in texts[:2]) if index == 1 else texts[0].text
        metadata.append({"number": index, "title": title})
(public / "slides.json").write_text(json.dumps(metadata, indent=2), encoding="utf-8")
print(f"Prepared {len(slides)} full-size slide images and thumbnails")
