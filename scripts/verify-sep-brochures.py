from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import xml.etree.ElementTree as ET
import re
import json
from PIL import Image, ImageDraw
from pypdf import PdfReader

root=Path(__file__).resolve().parents[1]
out=root/'output/pdf/SEP-Brochures'
pdf=PdfReader(out/'SEP-Complete-Module-Brochures.pdf')
assert len(pdf.pages)==8
normalize=lambda text: re.sub(r'[^a-z0-9]','',text.lower())
alltext=normalize(' '.join(page.extract_text() for page in pdf.pages))
with ZipFile(r'C:\Users\LENOVO\Downloads\AllAttachments\Arcteon Digital-SEP.pptx') as source:
    for i in range(2,22):
        values=[item.text or '' for item in ET.fromstring(source.read(f'ppt/slides/slide{i}.xml')).findall('.//{http://schemas.openxmlformats.org/drawingml/2006/main}t')][4:]
        for value in values:
            assert value.isdigit() or normalize(value) in alltext, (i,value)
    contacts=[item.text or '' for item in ET.fromstring(source.read('ppt/slides/slide22.xml')).findall('.//{http://schemas.openxmlformats.org/drawingml/2006/main}t')][2:]
    for value in contacts:
        assert normalize(value.replace('https://','').replace('/','')) in alltext, value

boxes=json.loads((root/'tmp/pdfs/sep-brochures/text-layout.json').read_text())
for i,a in enumerate(boxes):
    for b in boxes[i+1:]:
        if a['page'] != b['page']: continue
        x_overlap=min(a['x']+a['width'],b['x']+b['width'])-max(a['x'],b['x'])
        y_overlap=min(a['top']+a['height'],b['top']+b['height'])-max(a['top'],b['top'])
        assert x_overlap<=2 or y_overlap<=2,(a,b)

slugs=[path.stem for path in sorted(out.glob('[0-9][0-9]-*.pdf'))]
assert len(slugs)==8
sheet=Image.new('RGB',(1200,1720),'#E7EEF4')
draw=ImageDraw.Draw(sheet)
for i,slug in enumerate(slugs,1):
    png=out/f'{slug}.png'
    temporary=out/f'spread-{i}.png'
    if temporary.exists(): temporary.replace(png)
    image=Image.open(png)
    assert image.size==(2400,1600)
    image.thumbnail((576,384),Image.Resampling.LANCZOS)
    x=12+((i-1)%2)*600; y=12+((i-1)//2)*430
    sheet.paste(image,(x,y))
    draw.text((x,y+392),slug.replace('-',' '),fill='#07366A')
sheet.save(root/'tmp/pdfs/sep-brochures/final-contact-sheet.jpg',quality=90)

readme='''SEP BROCHURE SET

Eight three-panel brochure spreads inspired by the supplied ByondQ brochure.
Includes all six core SEP modules, a platform overview, and full device deployment workflows.
All 22 source slides are covered. Features, benefits and workflow steps are preserved.

The combined PDF contains all eight spreads. Individual PDFs are vector-text versions for sharing and printing.
PNG images are 2400 x 1600 pixels. These are digital brochure spreads; a printer should prepare fold order,
bleed and any required production settings before physical tri-fold printing.

Source: Arcteon Digital-SEP.pptx supplied by the user.
Reference visual style: ByondQ - Smart Queue Banking Brochure.png supplied by the user.
'''
(out/'Read-me.txt').write_text(readme,encoding='utf-8')
archive=root/'output/pdf/SEP-Brochure-Set.zip'
with ZipFile(archive,'w',ZIP_DEFLATED) as bundle:
    for path in sorted(out.iterdir()):
        if path.is_file(): bundle.write(path,path.name)
print('PASS: all module features, benefits and process text retained; source contact details retained; no overlapping text; eight valid 2400x1600 PNGs.')
print(archive)
