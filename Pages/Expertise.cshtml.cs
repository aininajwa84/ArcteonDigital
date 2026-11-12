using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dewi.Pages;

public record ServiceArea(string Title, string Description);

public class ExpertiseModel : PageModel
{
    public Service CurrentService { get; private set; } = null!;
    public IReadOnlyList<Service> Services => IndexModel.ServiceCatalogue;
    public ServiceArea? Example => CurrentService.Key switch
    {
        "artificial-intelligence" => new("Compare supplier quotations faster",
            "When suppliers send quotations in different formats, AI can extract prices, delivery timelines and payment terms into a side-by-side summary. Your team can review the original quotations and decide which option best fits your needs."),
        "software-customization" => new("Record stock movements with barcode scanning",
            "If your inventory system relies on staff typing in product codes, we can build a barcode-scanning screen that records items as they arrive or leave. The screen can connect to your existing stock records so staff can check quantities without entering the same information twice."),
        "digital-transformation" => new("Plan a better approval process",
            "If your team tracks approvals across spreadsheets and email, we can map how purchase requests move today, identify where they get stuck and recommend a shared workflow. Your roadmap can show what to pilot first and what to check before expanding to other request types."),
        _ => null
    };

    public IReadOnlyList<ServiceArea> Areas => CurrentService.Number switch
    {
        "01" => [
            new("AI opportunity assessment", "Identify useful AI applications in your business, review available data and prioritise where to start."),
            new("AI assistants and document processing", "Explore knowledge search, document processing and assistants that help your team find and use information."),
            new("Workflow automation and integration", "Plan how AI can support routine tasks and connect to your existing systems and processes.")
        ],
        "02" => [
            new("Requirements and workflow design", "Define what users need to do, what information they need, and which workflows the software must support."),
            new("Custom features and applications", "Build new applications or extend existing software with features such as forms, dashboards or reporting."),
            new("System and API integration", "Connect applications through APIs so they can share records and reduce duplicate data entry.")
        ],
        "03" => [
            new("Process and system review", "Understand your current workflows, existing systems and opportunities to reduce manual work."),
            new("Recommendations for digital workflows", "Evaluate where digital tools, automation or integration would help and recommend changes that fit your team’s needs."),
            new("Transformation roadmap", "Set priorities, identify who should own each change and outline a phased rollout.")
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
