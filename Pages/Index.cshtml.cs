using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dewi.Pages;

public record Service(string Number, string Icon, string Title, string Description, string Detail)
{
    public string Key => Number switch
    {
        "01" => "artificial-intelligence",
        "02" => "software-customization",
        "03" => "digital-transformation",
        _ => throw new InvalidOperationException("Unknown service")
    };
}
public record BusinessSolution(string Key, string Icon, string Category, string Title, string Description, string[] Features, string LogoClass)
{
    public bool IsClientSolution => Key is "tender" or "rappovena";
    public string LogoAlt => Key == "clinic" ? "ArcteonMed logo" : $"{Title} logo";
    public string? BannerLogoFile => Key switch
    {
        "queue" => "byondq-banner-source.png",
        "nexus" => "arcteon-nexus-wordmark-light.svg",
        _ => LogoFile
    };
    public string? LogoFile => Key switch
    {
        "hr" => "workvibes-symbol.svg",
        "clinic" => "arcteonmed-transparent.png",
        "assets" => "sep-symbol-transparent.png",
        "queue" => "byondq-logo.png",
        "nexus" => "arcteon-nexus-wordmark.svg",
        "tender" => "tender-transparent.png",
        "rappovena" => "rappovena-transparent.png",
        _ => null
    };
}

public class IndexModel : PageModel
{
    public IReadOnlyList<Service> Services => ServiceCatalogue;
    public static IReadOnlyList<Service> ServiceCatalogue { get; } = [
        new("01", "bi-robot", "Artificial Intelligence (AI)", "Put artificial intelligence to work on real business challenges.", "Explore AI assistants, knowledge search, document processing and workflow automation. We work with you to identify practical use cases, assess your data and integrate AI into your existing processes."),
        new("02", "bi-code-square", "Software Customization Development", "Make your software fit the way your business works.", "Adapt existing applications, develop tailored features and connect your business systems. From workflow improvements to API integrations, we shape the solution around your requirements."),
        new("03", "bi-diagram-3", "Digital Transformation", "Connect your people, processes and technology.", "Review your current systems, identify manual processes and plan how digital tools can improve the way your business works. Build a roadmap for connected workflows, automation and software improvements aligned with your business goals.")
    ];

    public static IReadOnlyList<BusinessSolution> Catalogue { get; } = [
        new("hr", "bi-person-workspace", "HR MANAGEMENT SYSTEM", "WorkVibes", "Employee administration, attendance and everyday HR processes.", ["Employee administration", "Attendance workflows", "Everyday HR processes"], "product-workvibes"),
        new("clinic", "bi-heart-pulse", "CLINIC MANAGEMENT SYSTEM", "ArcteonMed", "A clinic-focused management solution supporting patient and clinic workflows.", ["Patient workflows", "Clinic administration", "Connected clinic processes"], "product-klinik"),
        new("assets", "bi-pc-display", "SOFTWARE LICENSING & COMPUTER MANAGEMENT", "Streamline EndPoint (SEP)", "Software licensing and computer management system.", ["Software licensing", "Computer management", "Device monitoring"], "product-streamline"),
        new("tender", "bi-diagram-3", "AI WORKFLOW PROCESSING & AUTOMATION", "Tender Quotation Management System", "AI workflow processing and automation for a client’s tender, quotation and procurement processes.", ["AI workflow processing", "Tender and quotation automation", "Procurement coordination"], "product-tender"),
        new("rappovena", "bi-globe2", "WEBSITE DEVELOPMENT", "RappoVena Website", "A corporate website developed for RappoVena, a medical supplier, to present its business online.", ["Corporate website development", "Business information", "Online presence"], "product-rappovena"),
        new("queue", "bi-person-lines-fill", "QUEUE MANAGEMENT SYSTEM", "ByondQ", "Organize customer flow, service queues and ticket operations.", ["Customer flow coordination", "Service queue management", "Ticket operations"], "product-queue"),
        new("nexus", "bi-boxes", "BUSINESS TRANSFORMATION PLATFORM", "Arcteon Nexus", "Connect documents, customers, inventory, online sales and approval workflows in one modular platform.", ["Document Management", "CRM System", "Inventory System", "E-Commerce Platform", "Workflow Automation"], "product-nexus")
    ];
    public IReadOnlyList<BusinessSolution> Solutions => Catalogue;
    public IReadOnlyList<BusinessSolution> Products => Catalogue.Where(item => !item.IsClientSolution).ToArray();
    public IReadOnlyList<BusinessSolution> ClientSolutions => Catalogue.Where(item => item.IsClientSolution).ToArray();
    public void OnGet() { }
}
