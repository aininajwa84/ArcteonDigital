using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dewi.Pages.Forms;

[RequestSizeLimit(7 * 1024 * 1024)]
public class ApplyModel(IWebHostEnvironment environment) : PageModel
{
    public IActionResult OnGet() => NotFound();

    public async Task<IActionResult> OnPostAsync()
    {
        var form = await Request.ReadFormAsync();
        string Value(string key) => form[key].ToString().Trim();
        var name = Value("name");
        var email = Value("email");
        var phone = Value("phone");
        var role = Value("role");
        var interest = Value("interest");
        var organization = Value("organization");
        var message = Value("message");
        var startDate = Value("startDate");
        IActionResult Error(string text) => Content(text, "text/plain");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || email.Length > 254 ||
            !new EmailAddressAttribute().IsValid(email) || string.IsNullOrWhiteSpace(phone) || phone.Length > 40)
            return Error("Please enter your name, a valid email address and a phone number.");
        if (role is not ("Software Developer Intern" or "AI & Automation Intern" or "Software Engineer" or "General Interest") ||
            interest is not ("Internship" or "Full-time" or "General Interest"))
            return Error("Please select a valid role and interest.");
        if (organization.Length > 200 || message.Length > 10000)
            return Error("Keep the organization under 200 characters and cover note under 10000 characters.");
        if (startDate.Length > 0 && !DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return Error("Please enter a valid start date.");
        if (Value("consent") != "true") return Error("Please confirm that you agree to save this preview application locally.");
        var resume = form.Files.GetFile("resume");
        if (resume is null || resume.Length == 0 || resume.Length > 5 * 1024 * 1024)
            return Error("Upload a PDF, DOC or DOCX résumé of up to 5 MB.");
        var extension = Path.GetExtension(resume.FileName).ToLowerInvariant();
        if (extension is not (".pdf" or ".doc" or ".docx")) return Error("Only PDF, DOC and DOCX résumés are accepted.");
        await using var input = resume.OpenReadStream();
        var signature = new byte[8];
        var read = await input.ReadAtLeastAsync(signature, signature.Length, throwOnEndOfStream: false);
        var valid = extension switch
        {
            ".pdf" => read >= 5 && signature.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".doc" => read >= 8 && signature.AsSpan().SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            ".docx" => read >= 4 && signature.AsSpan(0, 4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            _ => false
        };
        if (!valid) return Error("The résumé contents do not match the selected file type.");
        var directory = Path.Combine(environment.ContentRootPath, "App_Data", "applications");
        Directory.CreateDirectory(directory);
        var id = Guid.NewGuid().ToString("N");
        var resumePath = Path.Combine(directory, id + extension);
        try
        {
            await using (var output = System.IO.File.Create(resumePath))
            {
                await output.WriteAsync(signature.AsMemory(0, read));
                await input.CopyToAsync(output);
            }
            var application = new { Name = name, Email = email, Phone = phone, Role = role, Interest = interest,
                Organization = organization, StartDate = startDate, Message = message,
                ResumeFile = id + extension, CreatedUtc = DateTimeOffset.UtcNow, Preview = true };
            await System.IO.File.WriteAllTextAsync(Path.Combine(directory, id + ".json"),
                JsonSerializer.Serialize(application, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            System.IO.File.Delete(resumePath);
            throw;
        }
        return Content("OK", "text/plain");
    }
}
