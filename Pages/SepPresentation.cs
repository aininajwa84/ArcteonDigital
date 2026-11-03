namespace Dewi.Pages;

public record SepBrochure(string File, string Title);

public static class SepPresentation
{
    // These spreads cover all 22 source slides, including each of the six modules.
    public static IReadOnlyList<SepBrochure> Brochures { get; } = [
        new("01-Platform-Overview", "Platform overview"),
        new("02-Endpoint-Management", "Endpoint Management"),
        new("03-IT-Service-Desk", "IT Service Desk"),
        new("04-System-Monitoring", "System Monitoring"),
        new("05-Smart-Analytics", "Smart Analytics"),
        new("06-IT-Asset-Management", "IT Asset Management"),
        new("07-Workflow-Automation", "Workflow Automation & Preventive Maintenance"),
        new("08-Device-Deployment", "Device deployment, tagging & checklists")
    ];
}
