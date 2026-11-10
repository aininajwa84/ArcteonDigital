"""Create the Nexus brochure from the product presentation's five modules."""

from pathlib import Path
from xml.sax.saxutils import escape

from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.platypus import Paragraph


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output/pdf/arcteon-nexus-brochure.pdf"
WIDTH, HEIGHT = 1080, 720
NAVY = "#102F4B"
ORANGE = "#FF8737"
INK = "#173548"
MUTED = "#496376"

MODULES = [
    ("Document Management", "Digitise paper files, search by name, date or reference, and use AI to find information in cloud records."),
    ("CRM System", "Keep customer history together, track enquiries and sales, and set follow-up reminders."),
    ("Inventory System", "Check stock balances, scan barcodes, track stock movements and review inventory reports."),
    ("E-Commerce Platform", "Promote products online, accept orders, issue bills and receipts, and connect a payment gateway."),
    ("Workflow Automation", "Use digital forms, route approvals, notify staff and keep an approval history."),
]

STEPS = [
    ("Discover", "Identify the process that takes the most time or causes the most friction."),
    ("Start", "Choose the module that addresses that problem and fit it to your workflow."),
    ("Connect", "Link the next process when your team is ready to share information across modules."),
    ("Grow", "Train your team and refine the setup as your needs change."),
]


def build():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    pdfmetrics.registerFont(TTFont("Nexus", "C:/Windows/Fonts/arial.ttf"))
    pdfmetrics.registerFont(TTFont("Nexus-Bold", "C:/Windows/Fonts/arialbd.ttf"))
    pdf = canvas.Canvas(str(OUTPUT), pagesize=(WIDTH, HEIGHT), pageCompression=1)
    pdf.setTitle("Arcteon Nexus | Product Brochure")
    pdf.setAuthor("Arcteon Digital")

    def rect(x, top, width, height, fill, radius=0):
        pdf.setFillColor(fill)
        pdf.roundRect(x, HEIGHT - top - height, width, height, radius, fill=1, stroke=0)

    def text(value, x, top, width, size, color=INK, bold=False):
        style = ParagraphStyle(
            "Nexus text", fontName="Nexus-Bold" if bold else "Nexus",
            fontSize=size, leading=size * 1.27, textColor=color,
        )
        paragraph = Paragraph(escape(value).replace("\n", "<br/>"), style)
        _, height = paragraph.wrap(width, HEIGHT)
        assert top + height <= HEIGHT, value
        paragraph.drawOn(pdf, x, HEIGHT - top - height)
        return top + height

    rect(0, 0, 360, HEIGHT, NAVY)
    rect(360, 0, 360, HEIGHT, "#FFFFFF")
    rect(720, 0, 360, HEIGHT, "#F0F5F8")
    rect(34, 46, 38, 4, ORANGE, 2)
    text("ARCTEON DIGITAL", 34, 64, 292, 12, "#DFEAF2", True)
    text("Arcteon\nNexus", 34, 112, 292, 58, "#FFFFFF", True)
    text("Connected Business\nTransformation Platform", 34, 277, 292, 22, ORANGE, True)
    text("Start with one business process and connect more when your team is ready.", 34, 359, 279, 15, "#DFEAF2")

    # Connect the five module numbers to illustrate how they fit together.
    pdf.setStrokeColor("#50718A")
    pdf.setLineWidth(2)
    pdf.line(56, 190, 304, 190)
    for index in range(5):
        x = 56 + index * 62
        pdf.setFillColor(ORANGE if index == 0 else "#2C4E6A")
        pdf.circle(x, 190, 17, stroke=0, fill=1)
        pdf.setFont("Nexus-Bold", 11)
        pdf.setFillColor(NAVY if index == 0 else "#FFFFFF")
        pdf.drawCentredString(x, 186, f"{index + 1:02}")
    text("Five modules. One platform.", 34, 577, 292, 18, "#FFFFFF", True)
    text("Empowering Your Digital Transformation Journey", 34, 655, 292, 10, "#DFEAF2")

    text("THE PLATFORM", 390, 46, 300, 11, MUTED, True)
    text("Five business modules", 390, 69, 300, 25, INK, True)
    for index, (title, description) in enumerate(MODULES):
        top = 126 + index * 106
        if index:
            rect(390, top - 13, 300, 1, "#DFE8EF")
        text(f"{index + 1:02}", 390, top + 3, 27, 12, MUTED, True)
        end = text(title, 425, top, 265, 16, INK, True)
        end = text(description, 425, end + 6, 265, 12.5, MUTED)
        assert end < top + 93, title

    text("GETTING STARTED", 750, 46, 300, 11, MUTED, True)
    text("From one process to\nconnected operations", 750, 69, 300, 25, INK, True)
    for index, (title, description) in enumerate(STEPS):
        top = 158 + index * 91
        rect(750, top + 3, 3, 18, ORANGE, 1)
        end = text(title, 763, top, 287, 16, INK, True)
        end = text(description, 763, end + 5, 287, 12.5, MUTED)
        assert end < top + 83, title

    rect(750, 537, 300, 136, "#DFEBF3", 12)
    text("Discuss Arcteon Nexus", 767, 553, 265, 16, INK, True)
    text("enquiry@arcteondigital.com.my", 767, 589, 265, 12, NAVY)
    text("+603 9226 8079", 767, 616, 265, 12, NAVY)
    text("arcteon-digital.com", 767, 643, 265, 12, NAVY)
    pdf.linkURL("mailto:enquiry@arcteondigital.com.my", (767, 114, 1032, 132), relative=0)
    pdf.linkURL("tel:+60392268079", (767, 87, 1032, 105), relative=0)
    pdf.linkURL("https://arcteon-digital.com/", (767, 60, 1032, 78), relative=0)
    pdf.save()


if __name__ == "__main__":
    build()
