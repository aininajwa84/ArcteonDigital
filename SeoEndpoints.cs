using System.Security;
using System.Text;
using Dewi.Pages;
using Dewi.Seo;

namespace Dewi;

public static class SeoEndpoints
{
    // Crawlers used by AI search/assistants. Remove a name here to stop allowing it.
    private static readonly string[] AiCrawlers =
        ["GPTBot", "OAI-SearchBot", "ChatGPT-User", "ClaudeBot", "Claude-SearchBot", "PerplexityBot", "Google-Extended"];

    public static void MapSeoEndpoints(this WebApplication app)
    {
        app.MapGet("/robots.txt", (HttpContext http, IConfiguration config) =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Disallow: /forms/");
            sb.AppendLine("Disallow: /admin/");
            sb.AppendLine("Disallow: /Error");
            sb.AppendLine("Disallow: /starter-page");
            sb.AppendLine("Disallow: /portfolio-details");
            sb.AppendLine();
            foreach (var bot in AiCrawlers)
            {
                sb.AppendLine($"User-agent: {bot}");
                sb.AppendLine("Allow: /");
                sb.AppendLine();
            }
            sb.AppendLine($"Sitemap: {BaseUrl(http, config)}/sitemap.xml");
            return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
        });

        app.MapGet("/sitemap.xml", (HttpContext http, IConfiguration config, BlogStore blog) =>
        {
            var baseUrl = BaseUrl(http, config);
            var paths = new List<(string Path, DateTime? LastMod)> { ("/", null), ("/careers", null), ("/blog", null) };
            paths.AddRange(IndexModel.ServiceCatalogue.Select(s => ($"/services/{s.Key}", (DateTime?)null)));
            paths.AddRange(IndexModel.Catalogue.Select(s => ($"/service-details?solution={Uri.EscapeDataString(s.Key)}", (DateTime?)null)));
            paths.AddRange(blog.Published().Select(p => ($"/blog/{p.Slug}", (DateTime?)(p.PublishedUtc ?? p.CreatedUtc))));

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            foreach (var (path, lastMod) in paths)
                sb.AppendLine($"  <url><loc>{SecurityElement.Escape(baseUrl + path)}</loc>" +
                    (lastMod is { } d ? $"<lastmod>{d:yyyy-MM-dd}</lastmod>" : "") + "</url>");
            sb.AppendLine("</urlset>");
            return Results.Text(sb.ToString(), "application/xml; charset=utf-8");
        });

        app.MapGet("/llms.txt", (HttpContext http, IConfiguration config, BlogStore blog) =>
        {
            var baseUrl = BaseUrl(http, config);
            var sb = new StringBuilder();
            sb.AppendLine("# Arcteon Digital");
            sb.AppendLine();
            sb.AppendLine("> Arcteon Digital Sdn Bhd is a technology company in Cheras, Kuala Lumpur, Malaysia. We provide AI solutions, custom software development and digital transformation consulting for businesses.");
            sb.AppendLine();
            sb.AppendLine("Contact: enquiry@arcteondigital.com.my | +603-92268079");
            sb.AppendLine();
            sb.AppendLine("## Services");
            foreach (var s in IndexModel.ServiceCatalogue)
                sb.AppendLine($"- [{s.Title}]({baseUrl}/services/{s.Key}): {s.Description}");
            sb.AppendLine();
            sb.AppendLine("## Solutions and products");
            foreach (var s in IndexModel.Catalogue)
                sb.AppendLine($"- [{s.Title}]({baseUrl}/service-details?solution={Uri.EscapeDataString(s.Key)}): {s.Description}");
            sb.AppendLine();
            var posts = blog.Published();
            if (posts.Count > 0)
            {
                sb.AppendLine("## Articles");
                foreach (var p in posts)
                    sb.AppendLine($"- [{p.Title}]({baseUrl}/blog/{p.Slug}): {p.Description}");
                sb.AppendLine();
            }
            sb.AppendLine("## Company");
            sb.AppendLine($"- [Home]({baseUrl}/): About Arcteon Digital and contact details");
            sb.AppendLine($"- [Careers]({baseUrl}/careers): Internships and software careers in Kuala Lumpur");
            return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
        });
    }

    private static string BaseUrl(HttpContext http, IConfiguration config) =>
        (config["Site:BaseUrl"] ?? $"{http.Request.Scheme}://{http.Request.Host}").TrimEnd('/');
}
