using System.Globalization;
using System.IO.Compression;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;
using MimeKit;

namespace Dewi.Email;

public static class WebsiteForms
{
    private const int MaxDocumentBytes = 5 * 1024 * 1024;
    private const int MaxSupportingDocuments = 3;
    private const int MaxAttachmentBytes = 15 * 1024 * 1024;
    private const int FormOverheadBytes = 128 * 1024;

    public static void MapWebsiteForms(this WebApplication app)
    {
        var forms = app.MapGroup("/forms").RequireRateLimiting("website-forms");
        forms.MapPost("/contact", (HttpContext context, IAntiforgery antiforgery, SmtpSender sender) =>
            SubmitAsync(context, antiforgery, sender, career: false));
        forms.MapPost("/applications", (HttpContext context, IAntiforgery antiforgery, SmtpSender sender) =>
            SubmitAsync(context, antiforgery, sender, career: true));
    }

    private static async Task<IResult> SubmitAsync(HttpContext context, IAntiforgery antiforgery,
        SmtpSender sender, bool career)
    {
        var request = context.Request;
        var cancellationToken = context.RequestAborted;
        var sizeLimit = FormOverheadBytes + (career ? MaxAttachmentBytes : 0);
        var sizeError = career ? "Attachments must be 15 MB or smaller in total." : "The submission is too large.";
        if (!request.HasFormContentType)
            return Error("Please submit the website form.");
        if (request.ContentLength > sizeLimit)
            return Error(sizeError, 413);
        if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
            limit.MaxRequestBodySize = sizeLimit;

        IFormCollection form;
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (AntiforgeryValidationException)
        {
            return Error("Please refresh the page and submit the form again.");
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == 413)
        {
            return Error(sizeError, 413);
        }
        catch (InvalidDataException)
        {
            return Error("The form could not be read. Please check your fields and try again.");
        }

        var name = Value(form, "name");
        var email = Value(form, "email");
        var message = Value(form, "message");
        if (!IsLine(name, 200) || !IsLine(email, 254) ||
            !MailboxAddress.TryParse(email, out var replyTo) ||
            !string.Equals(replyTo.Address, email, StringComparison.OrdinalIgnoreCase))
            return Error("Please enter your name and a valid email address.");

        if (message.Length > 10000)
            return Error("Please keep your message under 10,000 characters.");

        string subject;
        var body = new BodyBuilder();
        if (career)
        {
            var role = Value(form, "role");
            var interest = role == CareerRoles.GeneralInterest ? CareerRoles.GeneralInterest : Value(form, "interest");
            var phone = Value(form, "phone");
            var organization = Value(form, "organization");
            if (CareerRoles.ApplicationType(role) is null || !IsLine(phone, 40) ||
                (role != CareerRoles.GeneralInterest && interest is not ("Internship" or "Full-time")) ||
                (organization.Length > 0 && !IsLine(organization, 200)) ||
                Value(form, "consent") != "true")
                return Error("Please complete the required career application fields and confirm your consent.");

            var details = new List<(string Label, string Value)>
            {
                ("Full name", name),
                ("Email address", email),
                ("Phone number", phone),
                ("Application type", interest)
            };
            if (organization.Length > 0)
                details.Add(("University / organization", organization));

            if (interest == "Internship")
            {
                var startDate = Value(form, "startDate");
                var endDate = Value(form, "endDate");
                if (!IsOptionalDate(startDate) || !IsOptionalDate(endDate))
                    return Error("Please enter valid internship dates.");
                if (startDate.Length > 0 && endDate.Length > 0 && string.CompareOrdinal(endDate, startDate) < 0)
                    return Error("The internship end date must be on or after the start date.");

                if (startDate.Length > 0)
                    details.Add(("Available start date", FormatDate(startDate)));
                if (endDate.Length > 0)
                    details.Add(("Internship end date", FormatDate(endDate)));
            }

            var resumes = form.Files.GetFiles("resume");
            var supportingDocuments = form.Files.GetFiles("supportingDocuments");
            if (resumes.Count != 1 || resumes[0].Length is <= 0 or > MaxDocumentBytes)
                return Error("Please attach one résumé that is 5 MB or smaller.");
            if (supportingDocuments.Count > MaxSupportingDocuments || form.Files.Count != resumes.Count + supportingDocuments.Count)
                return Error("Please attach up to three supporting documents.");
            if (supportingDocuments.Any(file => file.Length is <= 0 or > MaxDocumentBytes))
                return Error("Each supporting document must be non-empty and 5 MB or smaller.");
            if (form.Files.Sum(file => file.Length) > MaxAttachmentBytes)
                return Error(sizeError, 413);

            if (!await AttachDocumentAsync(body, resumes[0], "resume", cancellationToken))
                return Error("Please attach a valid PDF, DOC or DOCX résumé.");
            for (var i = 0; i < supportingDocuments.Count; i++)
            {
                if (!await AttachDocumentAsync(body, supportingDocuments[i], $"supporting-document-{i + 1}", cancellationToken))
                    return Error("Supporting documents must be valid PDF, DOC or DOCX files.");
            }

            subject = $"Career application | {role}";
            FormatCareerEmail(body, role, details, message, supportingDocuments.Count);
        }
        else
        {
            var enquirySubject = Value(form, "subject");
            if (!IsLine(enquirySubject, 300) || message.Length == 0 || form.Files.Count != 0)
                return Error("Please enter a subject and message.");
            subject = $"Website enquiry: {enquirySubject}";
            body.TextBody = $"Name: {name}\nEmail: {email}\n\n{message}";
        }

        if (!await sender.SendAsync(subject, replyTo, body, career, cancellationToken))
            return Error("Email delivery is temporarily unavailable. Please try again later.", 503);

        return Results.Text("OK");
    }

    private static void FormatCareerEmail(BodyBuilder body, string role,
        List<(string Label, string Value)> details, string coverNote, int supportingCount)
    {
        var attachments = supportingCount switch
        {
            0 => "Résumé / CV",
            1 => "Résumé / CV and 1 supporting document",
            _ => $"Résumé / CV and {supportingCount} supporting documents"
        };
        body.TextBody = $"ARCTEON DIGITAL CAREERS\nNew career application\n{role}\n\nApplicant details\n" +
            string.Join("\n", details.Select(detail => $"{detail.Label}: {detail.Value}")) +
            (coverNote.Length > 0 ? $"\n\nCover note\n{coverNote}" : "") +
            $"\n\nAttachments\n{attachments}\n\nReply to this email to contact the applicant.";

        var encoder = HtmlEncoder.Default;
        var rows = string.Concat(details.Select(detail => $"""
            <tr>
              <th scope="row" style="text-align:left;vertical-align:top;padding:9px 12px;color:#506679;border-bottom:1px solid #e1e8ee;font-weight:600;">{encoder.Encode(detail.Label)}</th>
              <td style="vertical-align:top;padding:9px 12px;border-bottom:1px solid #e1e8ee;overflow-wrap:anywhere;">{encoder.Encode(detail.Value)}</td>
            </tr>
            """));
        var note = coverNote.Length == 0 ? "" : $"""
            <h2 style="margin:24px 0 8px;font-size:16px;">Cover note</h2>
            <p style="margin:0;line-height:1.6;overflow-wrap:anywhere;">{encoder.Encode(coverNote.ReplaceLineEndings("\n")).Replace("\n", "<br>")}</p>
            """;
        body.HtmlBody = $"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;background:#f1f5f8;color:#162d40;font-family:Arial,sans-serif;">
              <div style="max-width:620px;margin:0 auto;background:#ffffff;">
                <div style="background:#142e43;color:#ffffff;padding:24px;border-bottom:3px solid #f47845;">
                  <p style="margin:0 0 8px;color:#ffb591;font-size:12px;font-weight:bold;">ARCTEON DIGITAL CAREERS</p>
                  <h1 style="margin:0 0 6px;font-size:24px;">New career application</h1>
                  <p style="margin:0;font-size:16px;overflow-wrap:anywhere;">{encoder.Encode(role)}</p>
                </div>
                <div style="padding:24px;font-size:14px;line-height:1.5;">
                  <h2 style="margin:0 0 10px;font-size:16px;">Applicant details</h2>
                  <table style="width:100%;border-collapse:collapse;background:#f5f8fa;">{rows}</table>
                  {note}
                  <h2 style="margin:24px 0 8px;font-size:16px;">Attachments</h2>
                  <p style="margin:0;">{encoder.Encode(attachments)}</p>
                  <p style="margin:24px 0 0;color:#506679;font-size:13px;">Reply to this email to contact the applicant.</p>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string FormatDate(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    private static async Task<bool> AttachDocumentAsync(BodyBuilder body, IFormFile file, string fallbackName,
        CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();
        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        var extension = Path.GetExtension(fileName);
        var contentType = GetDocumentType(stream, extension.ToLowerInvariant());
        if (contentType is null)
            return false;
        stream.Position = 0;
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var safeStem = string.Concat(stem.Where(character => !char.IsControl(character))).Trim().TrimEnd('.');
        var attachmentName = (string.IsNullOrWhiteSpace(safeStem) ? fallbackName : safeStem) + extension;
        await body.Attachments.AddAsync(attachmentName, stream, contentType, cancellationToken);
        return true;
    }

    private static ContentType? GetDocumentType(Stream stream, string extension)
    {
        Span<byte> header = stackalloc byte[8];
        var length = stream.ReadAtLeast(header, header.Length, false);
        if (extension == ".pdf" && header[..length].StartsWith("%PDF-"u8))
            return new ContentType("application", "pdf");
        if (extension == ".doc" && header[..length].SequenceEqual(
            new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }))
            return new ContentType("application", "msword");
        if (extension != ".docx")
            return null;

        try
        {
            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return archive.GetEntry("[Content_Types].xml") is not null &&
                archive.GetEntry("word/document.xml") is not null && archive.GetEntry("word/vbaProject.bin") is null
                ? new ContentType("application", "vnd.openxmlformats-officedocument.wordprocessingml.document")
                : null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static string Value(IFormCollection form, string name) => form[name].ToString().Trim();
    private static bool IsOptionalDate(string value) =>
        value.Length == 0 || DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    private static bool IsLine(string value, int limit) =>
        value.Length > 0 && value.Length <= limit && !value.Any(char.IsControl);
    private static IResult Error(string message, int statusCode = 400) =>
        Results.Text(message, statusCode: statusCode);
}
