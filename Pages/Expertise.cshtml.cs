using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dewi.Pages;

public record ServiceArea(string Title, string Description);

public class ExpertiseModel : PageModel
{
    public Service CurrentService { get; private set; } = null!;
    public IReadOnlyList<Service> Services => IndexModel.ServiceCatalogue;
    public IReadOnlyList<ServiceArea> Areas => CurrentService.Number switch
    {
        "01" => [
            new("AI opportunity assessment", "Identify useful AI applications in your business, review available data and prioritise where to start."),
            new("AI assistants and document processing", "Explore knowledge search, document processing and assistants that help your team find and use information."),
            new("Workflow automation and integration", "Plan how AI can support routine tasks and connect to your existing systems and processes.")
        ],
        "02" => [
            new("Requirements and workflow design", "Review how your team works and define the changes your software needs to support."),
            new("Custom features and applications", "Adapt existing applications or develop features around your business requirements."),
            new("System and API integration", "Connect business systems and improve the flow of information between applications.")
        ],
        "03" => [
            new("Process and system review", "Understand your current workflows, existing systems and opportunities to reduce manual work."),
            new("Connected digital workflows", "Plan how digital tools, automation and system integration can connect teams and business processes."),
            new("Transformation roadmap", "Prioritise improvements and define a phased approach around your business goals, scope and delivery requirements.")
        ],
        _ => []
    };

    public IActionResult OnGet(string service)
    {
        if (service == "digital-consultation")
            return RedirectToPagePermanent("/Expertise", new { service = "digital-transformation" });
        var selected = Services.FirstOrDefault(item => item.Key == service);
        if (selected is null) return NotFound();
        CurrentService = selected;
        return Page();
    }
}
