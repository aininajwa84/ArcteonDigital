namespace Dewi.Seo;

/// <summary>Configuration for the automatic SEO content worker (section "Seo" in appsettings.json).</summary>
public sealed class SeoSettings
{
    /// <summary>Master switch. The worker does nothing while this is false.</summary>
    public bool Enabled { get; set; }

    /// <summary>Which AI writes and fact-checks: "Gemini" (free tier, default), "Groq" (free tier) or "Claude" (paid).</summary>
    public string Provider { get; set; } = "Gemini";

    /// <summary>API key. Prefer the environment variable (GEMINI_API_KEY, GROQ_API_KEY or ANTHROPIC_API_KEY) over a file.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Model name for the chosen provider. Empty uses that provider's default.</summary>
    public string Model { get; set; } = "";

    /// <summary>Overrides the Gemini endpoint. Only used for testing against a stand-in server.</summary>
    public string GeminiBaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    /// <summary>Password for /admin/seo (HTTP Basic, any user name). The admin area is disabled while empty.</summary>
    public string AdminPassword { get; set; } = "";

    /// <summary>Where generated posts are stored. Relative paths are under the app folder.</summary>
    public string DataPath { get; set; } = "App_Data/blog";

    /// <summary>Optional extra facts about the company that the writer may use (markdown or plain text).</summary>
    public string FactsFile { get; set; } = "Content/facts.md";

    /// <summary>Search phrases to write about, one article each, in order.</summary>
    public string[] Keywords { get; set; } = [];

    public int MaxPostsPerWeek { get; set; } = 2;
    public int CheckIntervalHours { get; set; } = 12;
    public int MinWords { get; set; } = 600;

    /// <summary>Stop writing new drafts while this many held drafts are waiting for review.</summary>
    public int MaxHeldDrafts { get; set; } = 3;

    /// <summary>When false (default), every draft is held for your approval in /admin/seo instead of publishing itself.</summary>
    public bool AutoPublish { get; set; }

    /// <summary>Email a copy of each published or held post to the contact recipient.</summary>
    public bool EmailNotifications { get; set; } = true;
}
