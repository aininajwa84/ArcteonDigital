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

    public IReadOnlyList<(string Question, string Answer)> Faqs => CurrentService.Key switch
    {
        "artificial-intelligence" => [
            ("What can AI do for my business?", "AI can support tasks such as knowledge search, document processing, assistants and workflow automation. We start by identifying practical use cases and reviewing your data before recommending where to begin."),
            ("Do I need to replace my existing systems to use AI?", "No. We plan how AI can connect to your existing systems and processes, so your team can keep working in the tools it already uses."),
            ("How do we get started?", "Tell us about your workflow and the work you want to simplify. Scope, integrations and delivery timelines are confirmed during discovery.")
        ],
        "software-customization" => [
            ("Can you add features to software we already use?", "Yes. We can build a new application, extend an existing system with features such as forms, dashboards or reporting, or connect systems through APIs so staff enter information once."),
            ("How do you decide what to build?", "We begin with requirements and workflow design: what users need to do, what information they need and which workflows the software must support."),
            ("How long does a custom software project take?", "Scope, integrations and delivery timelines are confirmed during discovery, once we understand your requirements.")
        ],
        "digital-transformation" => [
            ("What does a digital transformation consultation involve?", "We review how your team works, identify where manual work or duplication slows it down and decide which changes to prioritise. Together we outline a phased roadmap for the rollout."),
            ("Do we need to change everything at once?", "No. The roadmap is phased. It sets priorities and shows what to pilot first and what to check before expanding."),
            ("Who is this for?", "Businesses whose teams track work across spreadsheets, email or disconnected systems and want to know where to start.")
        ],
        _ => []
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
