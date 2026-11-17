using System.Text;
using Dewi.Pages;
using Microsoft.Extensions.Options;

namespace Dewi.Seo;

/// <summary>The only facts the writer may use about Arcteon Digital. Anything outside it fails the fact-check.</summary>
public sealed class FactSheet(IOptions<SeoSettings> options, IWebHostEnvironment env)
{
    public string Build()
    {
        var sb = new StringBuilder();
        sb.AppendLine("COMPANY");
        sb.AppendLine("- Arcteon Digital Sdn Bhd, a technology company in Cheras, Kuala Lumpur, Malaysia.");
        sb.AppendLine("- It provides AI solutions, custom software development and digital transformation consulting for businesses.");
        sb.AppendLine("- Contact: enquiry@arcteondigital.com.my, +603-92268079.");
        // Taken from the wording already on the website's service pages.
        sb.AppendLine("- Project scope, integrations and delivery timelines are confirmed during discovery.");
        sb.AppendLine("- Arcteon works with the customer to clarify requirements, review their current systems and agree on a practical next step.");
        sb.AppendLine();
        sb.AppendLine("SERVICES");
        foreach (var s in IndexModel.ServiceCatalogue)
            sb.AppendLine($"- {s.Title}: {s.Description} {s.Detail}");
        sb.AppendLine();
        sb.AppendLine("SOLUTIONS AND PRODUCTS");
        foreach (var s in IndexModel.Catalogue)
            sb.AppendLine($"- {s.Title} ({s.Category}): {s.Description} Features: {string.Join(", ", s.Features)}.");

        var path = options.Value.FactsFile;
        var file = Path.IsPathRooted(path) ? path : Path.Combine(env.ContentRootPath, path);
        if (File.Exists(file))
        {
            sb.AppendLine();
            sb.AppendLine("ADDITIONAL FACTS PROVIDED BY THE COMPANY");
            sb.AppendLine(File.ReadAllText(file));
        }
        return sb.ToString();
    }
}
