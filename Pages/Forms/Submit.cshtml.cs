using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dewi.Pages.Forms;

public class SubmitModel(IWebHostEnvironment environment) : PageModel
{
    public IActionResult OnGet() => NotFound();

    public async Task<IActionResult> OnPostAsync(string kind)
    {
        if (kind is not ("contact" or "newsletter" or "careers")) return NotFound();
        var form = await Request.ReadFormAsync();
        var email = form["email"].ToString().Trim();
        if (email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
            return Content("Please enter a valid email address.", "text/plain");
        var name = form["name"].ToString().Trim();
        var subject = form["subject"].ToString().Trim();
        var message = form["message"].ToString().Trim();
        if ((kind is "contact" or "careers") && (string.IsNullOrWhiteSpace(name) || name.Length > 200 ||
            string.IsNullOrWhiteSpace(subject) || subject.Length > 300 ||
            string.IsNullOrWhiteSpace(message) || message.Length > 10000))
            return Content("Enter a name (up to 200 characters), subject (up to 300) and message (up to 10000).", "text/plain");
        var portfolio = form["portfolio"].ToString().Trim();
        if (kind == "careers" && portfolio.Length > 0 &&
            (portfolio.Length > 2000 || !Uri.TryCreate(portfolio, UriKind.Absolute, out var portfolioUri) ||
             portfolioUri.Scheme is not ("https" or "http")))
            return Content("Please enter a valid http or https portfolio URL.", "text/plain");
        var directory = Path.Combine(environment.ContentRootPath, "App_Data", kind);
        Directory.CreateDirectory(directory);
        var submission = new { Email = email, Name = name, Subject = subject, Message = message, Portfolio = kind == "careers" ? portfolio : null, CreatedUtc = DateTimeOffset.UtcNow };
        await System.IO.File.WriteAllTextAsync(Path.Combine(directory, $"{Guid.NewGuid():N}.json"),
            JsonSerializer.Serialize(submission, new JsonSerializerOptions { WriteIndented = true }));
        return Content("OK", "text/plain");
    }
}
