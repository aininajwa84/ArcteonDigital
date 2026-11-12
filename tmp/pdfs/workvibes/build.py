from pathlib import Path

from reportlab.lib.colors import HexColor
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas

output = Path("output/pdf/WorkVibes-Brochure.pdf")
output.parent.mkdir(parents=True, exist_ok=True)
for name, file in [("Segoe", "segoeui.ttf"), ("Bold", "segoeuib.ttf")]:
    pdfmetrics.registerFont(TTFont(name, str(Path("C:/Windows/Fonts") / file)))

W, H = 1200, 795
c = canvas.Canvas(str(output), pagesize=(W, H), pageCompression=1)
c.setTitle("WorkVibes | Mobile App & Web Portal")
c.setAuthor("Arcteon Digital")
c.setSubject("WorkVibes product brochure for employees and HR administrators")

navy = "#13283B"
ink = "#172D41"
muted = "#475C6B"
orange = "#FF946B"
rust = "#9F381E"
cream = "#FFF5EF"
line = "#D9E3E9"
white = "#FFFFFF"

def box(x, y, w, h, fill, radius=0, stroke=None):
    c.setFillColor(HexColor(fill))
    c.setStrokeColor(HexColor(stroke or fill))
    c.setLineWidth(0.8)
    if radius:
        c.roundRect(x, H-y-h, w, h, radius, stroke=int(stroke is not None), fill=1)
    else:
        c.rect(x, H-y-h, w, h, stroke=int(stroke is not None), fill=1)

def text(value, x, y, size=14, color=ink, font="Segoe", spacing=0):
    obj = c.beginText(x, H-y-size*0.83)
    obj.setFont(font, size)
    obj.setFillColor(HexColor(color))
    obj.setCharSpace(spacing)
    obj.textOut(value)
    c.drawText(obj)

def label(value, x, y, color=rust):
    text(value, x, y, 10.5, color, "Bold", 1.4)

def copy(value, x, y, width, size=14, leading=19, color=muted):
    words = value.split()
    row = ""
    for word in words:
        candidate = f"{row} {word}".strip()
        if row and pdfmetrics.stringWidth(candidate, "Segoe", size) > width:
            text(row, x, y, size, color)
            y += leading
            row = word
        else:
            row = candidate
    if row:
        text(row, x, y, size, color)
        y += leading
    return y

def dot(x, y, radius, color):
    c.setFillColor(HexColor(color))
    c.circle(x, H-y, radius, fill=1, stroke=0)

def features(x, entries, start=230):
    y = start
    for number, (title, body) in enumerate(entries, 1):
        box(x, y, 30, 26, cream, 7)
        text(f"{number:02}", x+7, y+6, 11, rust, "Bold")
        text(title, x+41, y+2, 16, ink, "Bold")
        end = copy(body, x+41, y+26, 277, 13, 17)
        assert end <= y+79, title
        y += 79

# Three brochure panels with one shared contact strip.
box(0, 0, 400, 704, navy)
box(400, 0, 400, 704, "#FCFAF8")
box(800, 0, 400, 704, "#F2F7FA")
box(799.5, 0, 1, 704, "#DFE7EC")
box(40, 39, 44, 44, white, 9)
c.drawImage(ImageReader("wwwroot/assets/img/products/workvibes-symbol-hd.png"),
            44, H-79, 36, 36, mask="auto", preserveAspectRatio=True, anchor="c")
text("WorkVibes", 95, 47, 28, white, "Bold")
label("EMPLOYEE MANAGEMENT SYSTEM", 41, 119, orange)
text("Your workday.", 39, 150, 45, white, "Bold")
text("In one place.", 39, 204, 45, white, "Bold")
copy("Track time, request leave and submit claims. Give HR teams a clearer view of workforce records.", 42, 278, 304, 16, 23, "#D3DFE9")

# Device illustration with abstract interface elements.
box(39, 404, 321, 238, "#1E3B51", 18)
box(67, 448, 214, 125, "#0F2131", 11, "#587488")
box(73, 454, 202, 113, "#E9F0F4", 6)
box(73, 454, 202, 21, "#D8E4EC", 6)
for x, colour in [(82, "#D17B71"), (90, "#D2996A"), (98, "#7CA3A4")]:
    dot(x, 464, 2.2, colour)
box(85, 487, 47, 65, "#D6E5EE", 5)
for i in range(3):
    box(92, 497+14*i, 25 if i == 0 else 31, 4, "#9AB0C1", 2)
for i, h in enumerate([23, 37, 31, 51, 40, 48]):
    box(147+i*13, 551-h, 8, h, orange if i == 3 else "#95AEC1", 3)
box(231, 434, 94, 150, "#102538", 14, "#678192")
box(237, 441, 82, 136, "#F8F1ED", 9)
box(266, 448, 24, 3, "#ADB7BF", 1.5)
dot(278, 482, 14, "#AF482F")
c.setStrokeColor(HexColor(white))
c.setLineWidth(1.5)
c.circle(278, H-482, 8, stroke=1, fill=0)
c.line(278, H-482, 278, H-477)
c.line(278, H-482, 282, H-482)
for i, w in enumerate([46, 54, 38]):
    box(251, 509+i*15, w, 6, "#BDCFD9" if i else "#E8AB97", 3)
label("MOBILE APP + WEB PORTAL", 75, 611, "#D7E6EF")

# Employee mobile app.
label("01 / MOBILE APP", 432, 44)
text("Manage your day", 431, 67, 30, ink, "Bold")
text("on the go", 431, 104, 30, ink, "Bold")
copy("Employee self-service for attendance, leave, claims and documents.", 432, 153, 308, 14, 20)
box(432, 207, 336, 1, line)
features(432, [
    ("Attendance & breaks", "Clock in and out, track lunch breaks and add a manual entry if you miss a clock-in."),
    ("Leave requests", "Apply for leave, attach supporting documents and check your request status."),
    ("Expense & mileage claims", "Submit receipt-based or mileage claims with attachments and track their status."),
    ("Dashboard & notifications", "See monthly attendance, expense and leave summaries and receive updates."),
    ("Documents & language", "View payslips, contracts and employment letters. Choose English, Bahasa Melayu or Chinese."),
])

# Administrator and employee web portal.
label("02 / WEB PORTAL", 832, 44)
text("Manage workforce", 831, 67, 30, ink, "Bold")
text("records", 831, 104, 30, ink, "Bold")
copy("HR and administrators get management tools. Employees can also access self-service online.", 832, 153, 319, 14, 20)
box(832, 207, 336, 1, line)
features(832, [
    ("Dashboard & timesheets", "Review attendance and workforce status. Filter timesheets and export to PDF or Excel."),
    ("Staff & company setup", "Register employees and manage departments, teams, job designations and company details."),
    ("Leave & expense settings", "Set leave types, expense approval levels and approvers, and car and motorcycle mileage rates."),
    ("Payslips & announcements", "Manage payslip records and attachments, and send staff notifications."),
    ("Employee self-service", "View your dashboard, manage attendance, submit leave and claims, and access notices, payslips and your profile."),
])

box(0, 704, W, 91, "#0E2030")
box(0, 704, W, 2, "#E77F5E")
text("Ask for a WorkVibes demo", 41, 726, 20, white, "Bold")
text("Arcteon Digital Sdn Bhd", 41, 755, 12, "#BFCEDA")
for x, value, target in [
    (432, "enquiry@arcteondigital.com.my", "mailto:enquiry@arcteondigital.com.my"),
    (762, "+603 9226 8079", "tel:+60392268079"),
    (962, "arcteon-digital.com", "https://arcteon-digital.com/"),
]:
    text(value, x, 742, 14, white, "Bold")
    width = pdfmetrics.stringWidth(value, "Bold", 14)
    c.linkURL(target, (x, H-760, x+width, H-739), relative=0)
c.showPage()
c.save()

