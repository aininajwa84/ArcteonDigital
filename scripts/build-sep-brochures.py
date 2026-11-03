"""Build a complete SEP brochure set from the supplied 22-slide deck.

The ByondQ reference informs the navy/orange three-panel visual language.
All features, benefits and process steps are read from the original deck.
"""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
from xml.sax.saxutils import escape
import xml.etree.ElementTree as ET
import json
from PIL import Image
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from pypdf import PdfReader, PdfWriter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'output/pdf/SEP-Brochures'
TMP = ROOT / 'tmp/pdfs/sep-brochures'
OUT.mkdir(parents=True, exist_ok=True)
TMP.mkdir(parents=True, exist_ok=True)
SOURCE = Path(r'C:\Users\LENOVO\Downloads\AllAttachments\Arcteon Digital-SEP.pptx')
W, H, PANEL, M = 1080, 720, 360, 26
NAVY, ORANGE, INK, MUTED = '#07366A', '#FF7918', '#102F4B', '#526779'
PALE, EDGE = '#F1F6FA', '#D7E2EB'
pdfmetrics.registerFont(TTFont('Brand', 'C:/Windows/Fonts/arial.ttf'))
pdfmetrics.registerFont(TTFont('Brand-Bold', 'C:/Windows/Fonts/arialbd.ttf'))
deck = ZipFile(SOURCE)
ns = {'a': 'http://schemas.openxmlformats.org/drawingml/2006/main'}
TEXT = {i: [x.text or '' for x in ET.fromstring(deck.read(f'ppt/slides/slide{i}.xml')).findall('.//a:t', ns)] for i in range(1,23)}
assert len([n for n in deck.namelist() if __import__('re').fullmatch(r'ppt/slides/slide\d+\.xml', n)]) == 22

def pairs(slide):
    values = TEXT[slide][4:]
    assert len(values) % 2 == 0, (slide, values)
    return list(zip(values[::2], values[1::2]))

def steps(slide):
    values = TEXT[slide][4:]
    if slide == 19: values = values[:-1]
    assert len(values) % 3 == 0
    return [values[i:i+3] for i in range(0,len(values),3)]

for name in ['image2.png','image5.png','image7.png','image8.png','image9.png','image10.png','image11.png','image12.png','image13.png','image14.png','image15.png','image16.png','image17.png','image18.png','image19.png']:
    (TMP/name).write_bytes(deck.read('ppt/media/'+name))

C = None
LOG = []
PAGE = 0
def para(text, x, top, width=308, size=11, color=INK, bold=False, leading=None):
    text = escape(str(text)).replace('\n','<br/>')
    style = ParagraphStyle('p', fontName='Brand-Bold' if bold else 'Brand', fontSize=size, leading=leading or size*1.38, textColor=color)
    p = Paragraph(text, style)
    _, height = p.wrap(width, 1000)
    assert top + height <= H-34, (PAGE, text, top, height)
    p.drawOn(C, x, H-top-height)
    LOG.append({'page': PAGE, 'text': text, 'x': x, 'top': top, 'width': width, 'height': height})
    return top+height

def rect(x,top,w,h,fill,stroke=None,r=0):
    C.setFillColor(fill)
    C.setStrokeColor(stroke or fill)
    if r: C.roundRect(x,H-top-h,w,h,r,fill=1,stroke=bool(stroke))
    else: C.rect(x,H-top-h,w,h,fill=1,stroke=bool(stroke))

def rule(x,top,w,color=ORANGE,thick=2):
    C.setStrokeColor(color); C.setLineWidth(thick); C.line(x,H-top,x+w,H-top)

def image(path,x,top,w,h,cover=False):
    im = Image.open(path).convert('RGBA')
    if cover:
        # Preserve original photographic pixels; crop only to fit the panel.
        if path.name == 'image2.png': im = im.crop((300,0,1017,1215))
        ratio=max(w/im.width,h/im.height)
        cw,ch=w/ratio,h/ratio
        left=(im.width-cw)/2; upper=(im.height-ch)/2
        im=im.crop((round(left),round(upper),round(left+cw),round(upper+ch)))
        C.drawImage(ImageReader(im),x,H-top-h,w,h,mask='auto')
    else:
        scale=min(w/im.width,h/im.height); iw,ih=im.width*scale,im.height*scale
        C.drawImage(ImageReader(im),x+(w-iw)/2,H-top-(h+ih)/2,iw,ih,mask='auto')

def icon(x,top,kind=0):
    y=H-top-15
    C.setFillColor(ORANGE); C.circle(x+15,y,15,fill=1,stroke=0)
    C.setStrokeColor('#FFFFFF'); C.setLineWidth(1.6)
    if kind in (0,2):
        C.roundRect(x+7,y-5,16,12,2,stroke=1,fill=0); C.line(x+11,y-9,x+19,y-9); C.line(x+15,y-5,x+15,y-9)
        if kind==2: C.line(x+10,y,x+13,y-2); C.line(x+13,y-2,x+19,y+4)
    elif kind==1:
        C.arc(x+7,y-5,x+23,y+11,0,180); C.roundRect(x+6,y-6,4,8,1,stroke=1,fill=0); C.roundRect(x+20,y-6,4,8,1,stroke=1,fill=0); C.line(x+22,y-6,x+17,y-10)
    elif kind==3:
        for j,hh in enumerate([6,11,16]): C.line(x+9+j*6,y-7,x+9+j*6,y-7+hh)
    elif kind==4:
        for dx,dy in [(8,4),(21,4),(14,-7)]: C.rect(x+dx-3,y+dy-3,6,6,stroke=1,fill=0)
        C.line(x+11,y+4,x+18,y+4); C.line(x+10,y+1,x+13,y-4); C.line(x+20,y+1,x+15,y-4)
    else:
        for dx,dy in [(8,6),(22,6),(15,-7)]: C.circle(x+dx,y+dy,3,stroke=1,fill=0)
        C.line(x+11,y+6,x+19,y+6); C.line(x+21,y+3,x+17,y-4)

def feature(title,body,x,top,width=308,kind=0,size=10.8):
    icon(x,top,kind)
    end=para(title,x+42,top+1,width-42,12,NAVY,True)
    end=para(body,x+42,end+5,width-42,size,MUTED)
    return max(top+34,end)+17

def brand(x,top=23):
    image(ROOT/'wwwroot/assets/img/products/sep-symbol-transparent.png',x,top,34,30)
    para('STREAMLINE END-POINT',x+44,top+1,250,12,NAVY,True)
    para('MANAGEMENT SYSTEM',x+44,top+18,250,8.4,MUTED,False)

def base(label,number):
    global PAGE
    PAGE=number
    rect(0,0,W,H,'#FFFFFF')
    rect(0,0,360,H,'#F6F9FC')
    rect(720,0,360,H,'#F8FBFD')
    for x in [360,720]:
        C.setStrokeColor(EDGE); C.setLineWidth(.7); C.line(x,34,x,H-24)
    # Curved navy/orange footer follows the supplied brochure's palette.
    rect(0,H-24,W,24,NAVY)
    path=C.beginPath(); path.moveTo(0,0); path.lineTo(W,0); path.lineTo(W,24); path.curveTo(690,24,360,35,0,28); path.close()
    C.setFillColor(NAVY); C.drawPath(path,stroke=0,fill=1)
    C.setFillColor(ORANGE); C.rect(0,23,360,3,fill=1,stroke=0)
    C.setFont('Brand',8); C.setFillColor('#FFFFFF'); C.drawString(26,8,'ARCTEON DIGITAL  /  STREAMLINE END-POINT')
    C.drawRightString(W-26,8,f'{number:02} / 08  |  {label.upper()}')

def section_title(title,x,top=35,subtitle=None):
    end=para(title,x,top,308,24,NAVY,True,28)
    rule(x,end+13,55)
    if subtitle: end=para(subtitle,x,end+28,308,11,MUTED)
    return end+34

def contact(x,top=527):
    rect(x,top,308,136,'#EAF1F7',r=12)
    para('Let’s discuss your IT operations',x+15,top+14,278,14,NAVY,True)
    para('Request a quotation or consultation.',x+15,top+39,278,10.2,MUTED)
    para('enquiry@arcteondigital.com.my',x+15,top+64,278,10.4,NAVY)
    para('+603 9226 8079',x+15,top+83,278,10.4,NAVY,True)
    para('arcteon-digital.com  |  Kuala Lumpur, Malaysia',x+15,top+105,278,9.1,MUTED)
    C.linkURL('mailto:enquiry@arcteondigital.com.my',(x+15,H-top-78,x+290,H-top-63),relative=0)
    C.linkURL('https://arcteon-digital.com/',(x+15,H-top-122,x+290,H-top-103),relative=0)

def screenshot(name,x,top,w=308,h=172,caption='SEP system screen from the product presentation'):
    rect(x-1,top-1,w+2,h+2,'#FFFFFF',EDGE,r=5)
    image(TMP/name,x+6,top+6,w-12,h-12)
    para(caption,x,top+h+8,w,8.3,MUTED)

def left_cover(module,title,subtitle,photo,impact,impactbody):
    brand(26)
    para(module,26,83,308,9.5,ORANGE,True)
    end=para(title,26,110,308,31,NAVY,True,34)
    end=para(subtitle,26,end+17,308,12,INK,False,17)
    assert end<282,(title,end)
    image(TMP/photo,0,294,360,230,True)
    # A light sweep ties the photo to the bottom benefit strip.
    p=C.beginPath(); p.moveTo(0,H-500); p.curveTo(105,H-543,240,H-505,360,H-522); p.lineTo(360,H-545); p.lineTo(0,H-545); p.close()
    C.setFillColor(NAVY); C.drawPath(p,fill=1,stroke=0)
    rect(0,525,360,143,NAVY)
    para(impact,26,545,308,18,'#FFFFFF',True,22)
    para(impactbody,26,599,308,11,'#E1ECF5')

MODULES = [
    dict(slug='02-Endpoint-Management', number='MODULE 01', title='Endpoint\nManagement', slides=[4,5], photo='image2.png', screen='image7.png', extra='image8.png', impact='A central view of\nyour endpoints', impactbody='Automatic records, live updates and information for operational decisions.'),
    dict(slug='03-IT-Service-Desk', number='MODULE 02', title='IT Service\nDesk', slides=[6,7], photo='image2.png', screen='image9.png', extra='image10.png', impact='Clearer requests.\nFaster diagnosis.', impactbody='Give employees a direct route to support and help IT teams prioritise cases.'),
    dict(slug='04-System-Monitoring', number='MODULE 03', title='System\nMonitoring', slides=[8,9], photo='image11.png', screen='image12.png', impact='Stay informed about\ndevice security', impactbody='Review security status, hardware changes and records for audit reporting.'),
    dict(slug='05-Smart-Analytics', number='MODULE 04', title='Smart\nAnalytics', slides=[10,11], photo='image13.png', screen='image14.png', impact='Identify issues\nsooner', impactbody='AI-assisted detection, automatic escalation and configurable monitoring.'),
    dict(slug='06-IT-Asset-Management', number='MODULE 05', title='IT Asset\nManagement', slides=[12,13], photo='image2.png', screen='image15.png', impact='Connected equipment.\nClearer accountability.', impactbody='Keep endpoints and their associated peripherals in a central equipment record.')
]
COVERAGE = {'01-Platform-Overview':[1,2,3,22]}
PDF = OUT/'SEP-Complete-Module-Brochures.pdf'
C=canvas.Canvas(str(PDF),pagesize=(W,H),pageCompression=1)
C.setTitle('Streamline End-Point | Complete Module Brochure Set')
C.setAuthor('Arcteon Digital')
C.setSubject('All six SEP modules, paperless approvals, preventive maintenance and complete device deployment workflows')

# 01: platform overview, all six module descriptions, value proposition and contact.
base('Platform overview',1)
left_cover('PRODUCT OVERVIEW','Streamline\nEnd-Point','Software licensing and computer management system.','image2.png','Simplify management.\nImprove efficiency.','Achieve optimal performance across daily IT operations.')
top=section_title('One platform.\nSix connected modules.',386,subtitle=TEXT[2][1])
modules=TEXT[3][4:]
for i in range(0,len(modules),3):
    _,title,body=modules[i:i+3]
    top=feature(title,body,386,top,kind=i//3,size=10.4)
top=section_title('Better visibility.\nLess manual work.',746)
for i,(title,body) in enumerate(pairs(2)):
    top=feature(title,body,746,top,kind=i,size=11)
assert top<515
contact(746)
C.showPage()

# 02-06: a complete feature and benefit spread for each core module.
for page,module in enumerate(MODULES,2):
    a,b=module['slides']; COVERAGE[module['slug']]=[a,b]
    base(TEXT[a][0],page)
    left_cover(module['number'],module['title'],TEXT[a][1],module['photo'],module['impact'],module['impactbody'])
    top=section_title('What the module does',386,subtitle=TEXT[a][1])
    for i,(title,body) in enumerate(pairs(a)):
        top=feature(title,body,386,top,kind=(page-2)%6)
    screen_top=max(350,top+10)
    screenshot(module['screen'],386,screen_top,h=172)
    if module.get('extra'):
        para('Operational view',386,screen_top+208,308,11,NAVY,True)
        image(TMP/module['extra'],386,screen_top+231,308,80)
    else:
        para('Connected to the SEP platform',386,screen_top+218,308,12,NAVY,True)
        para('This module forms part of the connected capabilities across the IT device lifecycle.',386,screen_top+242,308,11,MUTED)
    top=section_title('Why it matters',746,subtitle=TEXT[b][1])
    for i,(title,body) in enumerate(pairs(b)):
        top=feature(title,body,746,top,kind=i,size=10.6)
    assert top<521,(module['title'],top)
    contact(746)
    C.showPage()

# 07: workflow automation, forms, signatures, audit history and maintenance gate.
COVERAGE['07-Workflow-Automation']=[14,15,16]
base('Workflow automation',7)
brand(26)
para('MODULE 06',26,83,308,9.5,ORANGE,True)
para('Paperless\nworkflow\nautomation',26,110,308,31,NAVY,True,34)
para(TEXT[14][1],26,232,308,12,INK)
image(TMP/'image16.png',24,290,312,245)
rect(0,543,360,125,NAVY)
para('Workflow approvals.\nTraceable records.',26,560,308,18,'#FFFFFF',True,22)
para(pairs(14)[0][1],26,614,308,10.3,'#E1ECF5')
top=section_title('Digital forms\nand approvals',386,subtitle=TEXT[15][1])
for i,(title,body) in enumerate(pairs(15)):
    top=feature(title,body,386,top,kind=5)
assert top<635
para('Part of Workflow Automation',386,top+8,308,10,ORANGE,True)
top=section_title('Preventive\nMaintenance',746,subtitle=TEXT[16][1])
for number,title,body in steps(16):
    top=feature(number+'  '+title,body,746,top,kind=2,size=11)
para(pairs(14)[1][1],746,top+2,308,11,NAVY,True)
contact(746)
C.showPage()

# 08: complete manual and SEP processes, agent tagging and checklist verification.
COVERAGE['08-Device-Deployment']=[17,18,19,20,21]
base('Device deployment',8)
brand(26)
para('WORKFLOW AUTOMATION / DEPLOYMENT',26,83,308,9,ORANGE,True)
para('From arrival to\nproject closure',26,110,308,29,NAVY,True,33)
para(TEXT[17][1],26,192,308,11.5,MUTED)
top=241
for title,body in pairs(17):
    end=para(title,26,top,308,11,NAVY,True)
    top=para(body,26,end+4,308,10.2,MUTED)+13
rule(26,top+2,308,EDGE,.8)
top=para('SEP agent device tagging',26,top+18,308,14,NAVY,True)+10
for title,body in pairs(20):
    end=para(title,26,top,308,10.5,INK,True)
    top=para(body,26,end+3,308,9.8,MUTED)+9
top=para('Deployment checklist',26,top+6,308,14,NAVY,True)+9
for title,body in pairs(21):
    end=para(title,26,top,308,10.5,INK,True)
    top=para(body,26,end+3,308,9.8,MUTED)+8
assert top<674,top
top=section_title('Current deployment\nprocess',386,subtitle=TEXT[18][1])
for number,title,body in steps(18):
    rect(386,top,26,23,'#E6EDF3',r=4)
    para(number,390,top+4,20,10,NAVY,True)
    end=para(title,423,top,271,11,NAVY,True)
    top=para(body,423,end+3,271,10.1,MUTED)+17
assert top<664,top
top=section_title('Deployment\nwith SEP',746,subtitle=TEXT[19][1])
for number,title,body in steps(19):
    rect(746,top,26,23,ORANGE,r=4)
    para(number,750,top+4,20,10,'#FFFFFF',True)
    end=para(title,783,top,271,11,NAVY,True)
    top=para(body,783,end+3,271,10.1,MUTED)+17
rect(746,top+2,308,65,'#EAF1F7',r=8)
para(TEXT[19][-1],760,top+15,280,10.5,NAVY,True)
C.showPage()
C.save()

# Individual spreads are useful for sharing one module at a time.
reader=PdfReader(PDF)
assert len(reader.pages)==8
for i,slug in enumerate(COVERAGE):
    writer=PdfWriter(); writer.add_page(reader.pages[i])
    writer.add_metadata({'/Title': 'SEP - '+slug[3:].replace('-',' '), '/Author':'Arcteon Digital'})
    with (OUT/(slug+'.pdf')).open('wb') as f: writer.write(f)
assert sorted(n for values in COVERAGE.values() for n in values)==list(range(1,23))
(OUT/'Slide-Coverage.txt').write_text('SEP brochure content coverage\n\n'+'\n'.join(f'{slug}: source slides '+', '.join(map(str,values)) for slug,values in COVERAGE.items())+'\n\nAll 22 slides are covered. All six core modules have dedicated brochure spreads. Workflow Automation also includes digital forms, digital signatures, preventive maintenance, device deployment, device tagging and deployment checklists.\n',encoding='utf-8')
(TMP/'text-layout.json').write_text(json.dumps(LOG,indent=2),encoding='utf-8')
print(f'Created {PDF}: 8 brochure spreads; all 22 source slides covered.')
