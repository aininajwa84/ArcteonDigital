"""Create the Nexus brochure from the product presentation's five modules."""

from pathlib import Path
from xml.sax.saxutils import escape

from pypdf import PdfReader
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.platypus import Paragraph


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output/pdf/arcteon-nexus-brochure.pdf"
SOURCE = ROOT / "wwwroot/assets/brochures/nexus/arcteon-nexus-presentation.pdf"
WIDTH, HEIGHT = 1080, 720
NAVY = "#102F4B"
ORANGE = "#FF8737"
INK = "#173548"
MUTED = "#496376"

MODULES = [
    ("Document Management", "Digitise paper files and access them securely in the cloud. Search by name, date or reference, or use AI to find information faster."),
    ("CRM System", "Keep customer details and history together. Track enquiries and sales progress, and set reminders to follow up."),
    ("Inventory System", "Check stock balances, track items coming in and going out, scan barcodes and review stock reports."),
    ("E-Commerce Platform", "List products online, accept orders around the clock, issue bills and receipts, and connect a secure payment gateway."),
    ("Workflow Automation", "Use digital forms, route approvals to the right person, notify staff automatically and keep an approval history."),
]

STEPS = [
    ("Discover", "Review how your team works and identify the most urgent problem."),
    ("Start", "Choose the module that addresses that problem and adapt it to your process."),
    ("Connect", "Link the next workflow when your team is ready to share information across modules."),
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

    rect(0, 0, 360, 638, NAVY)
    rect(360, 0, 360, 638, "#FDFCFA")
    rect(720, 0, 360, 638, "#EEF4F8")
    rect(720, 0, 1, 638, "#DCE6ED")

    rect(31, 35, 29, 3, ORANGE, 1)
    text("ARCTEON DIGITAL", 31, 48, 298, 11, "#DFEAF2", True)
    text("Arcteon\nNexus", 29, 76, 300, 53, "#FFFFFF", True)
    text("Connected business\ntransformation platform", 31, 213, 298, 20, ORANGE, True)

    # Frame the right side of the original cover artwork, where the people and city are.
    cover = ImageReader(PdfReader(SOURCE).pages[0].images[0].image)
    source_width, source_height = cover.getSize()
    photo_x, photo_top, photo_width, photo_height = 31, 286, 298, 227
    scaled_width = source_width * photo_height / source_height
    photo_bottom = HEIGHT - photo_top - photo_height
    pdf.saveState()
    frame = pdf.beginPath()
    frame.roundRect(photo_x, photo_bottom, photo_width, photo_height, 12)
    pdf.clipPath(frame, stroke=0, fill=0)
    pdf.drawImage(cover, photo_x + photo_width - scaled_width, photo_bottom,
                  width=scaled_width, height=photo_height)
    pdf.restoreState()

    text("Start with one process", 31, 538, 298, 18, "#FFFFFF", True)
    text("Choose the module your team needs today and connect more as your operations grow.", 31, 569, 284, 14, "#DFEAF2")

    text("01 / THE PLATFORM", 391, 44, 298, 11, "#9C3B16", True)
    text("Five business modules", 391, 64, 298, 24, INK, True)
    text("Use each module on its own or connect it to the others as your needs change.", 391, 105, 294, 13, MUTED)
    for index, (title, description) in enumerate(MODULES):
        top = 163 + index * 91
        rect(391, top - 10, 298, 1, "#DFE8EF")
        rect(391, top + 1, 28, 25, "#FFECDD", 6)
        text(f"{index + 1:02}", 398, top + 6, 17, 11, "#9C3B16", True)
        end = text(title, 431, top, 258, 15, INK, True)
        end = text(description, 431, end + 4, 258, 12.3, MUTED)
        assert end < top + 81, title

    text("02 / GETTING STARTED", 751, 44, 298, 11, "#9C3B16", True)
    text("From one workflow\nto connected operations", 751, 64, 298, 24, INK, True)
    rect(751, 140, 298, 96, "#DDEAF2", 9)
    text("Share updates across teams", 765, 154, 270, 15, INK, True)
    text("When modules are connected, information from one step can support the next without teams maintaining separate records.", 765, 178, 270, 12.3, MUTED)

    for index, (title, description) in enumerate(STEPS):
        top = 267 + index * 82
        rect(751, top + 2, 3, 19, ORANGE, 1)
        end = text(title, 765, top, 280, 16, INK, True)
        end = text(description, 765, end + 5, 280, 12.5, MUTED)
        assert end < top + 72, title

    rect(0, 638, WIDTH, 82, "#0B2238")
    rect(0, 638, WIDTH, 2, ORANGE)
    text("Request a Nexus demo", 31, 659, 298, 19, "#FFFFFF", True)
    text("Arcteon Digital Sdn Bhd", 31, 685, 298, 11, "#DFEAF2")
    for x, label, url in [
        (391, "enquiry@arcteondigital.com.my", "mailto:enquiry@arcteondigital.com.my"),
        (701, "+603 9226 8079", "tel:+60392268079"),
        (883, "arcteon-digital.com", "https://arcteon-digital.com/"),
    ]:
        text(label, x, 673, 190, 12, "#FFFFFF", True)
        width = pdfmetrics.stringWidth(label, "Nexus-Bold", 12)
        pdf.linkURL(url, (x, HEIGHT - 690, x + width, HEIGHT - 673), relative=0)
    pdf.save()


if __name__ == "__main__":
    build()
