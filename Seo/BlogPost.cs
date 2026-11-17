namespace Dewi.Seo;

public sealed class BlogSection
{
    public string Heading { get; set; } = "";
    public List<string> Paragraphs { get; set; } = [];
    public List<string> Bullets { get; set; } = [];
}

public sealed class BlogFaq
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
}

public static class PostStatus
{
    public const string Published = "published";
    public const string Held = "held";
}

public sealed class BlogPost
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Keyword { get; set; } = "";
    public List<BlogSection> Sections { get; set; } = [];
    public List<BlogFaq> Faqs { get; set; } = [];
    public string Status { get; set; } = PostStatus.Held;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedUtc { get; set; }

    /// <summary>Why a draft was held (quality-gate and fact-check findings). Empty for clean posts.</summary>
    public List<string> Issues { get; set; } = [];

    public IEnumerable<string> AllText() =>
        Sections.SelectMany(s => new[] { s.Heading }.Concat(s.Paragraphs).Concat(s.Bullets))
            .Concat(Faqs.SelectMany(f => new[] { f.Question, f.Answer }));

    public int WordCount() =>
        Sections.SelectMany(s => s.Paragraphs.Concat(s.Bullets)).Sum(CountWords);

    public static int CountWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
