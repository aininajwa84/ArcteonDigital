using System.Text;
using System.Text.RegularExpressions;
using Dewi.Email;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Dewi.Seo;

/// <summary>Writer, then quality gate, then fact-checker, then publish-or-hold.</summary>
public sealed class ContentPipeline(
    ITextModel model,
    FactSheet facts,
    BlogStore store,
    IOptions<SeoSettings> options,
    IOptions<SmtpSettings> smtpOptions,
    IServiceScopeFactory scopes,
    IConfiguration config,
    ILogger<ContentPipeline> logger)
{
    private const string WriterSystem = """
        You are a content writer for Arcteon Digital Sdn Bhd, a technology company in Kuala Lumpur, Malaysia.
        You write helpful, original articles for business readers in Malaysia.

        STRICT RULES
        1. About Arcteon Digital, its services, products and location, use ONLY what the FACT SHEET says.
           Never invent clients, customers, case studies, prices, statistics, awards, certifications,
           partnerships, delivery timelines, integrations or product features.
        2. General, widely known industry advice is fine, but do not quote specific numbers, percentages,
           named studies, laws or standards unless the FACT SHEET contains them.
        3. Never promise or guarantee results. Say that scope and timelines are confirmed during discovery.
        4. Plain, professional English suited to Malaysian business readers. No hype, no filler.
        5. Output one JSON object and nothing else: no markdown, no code fences, no HTML tags.
        """;

    private const string CheckerSystem = """
        You are a strict fact-checker for a company website. You receive a FACT SHEET and an ARTICLE.
        List every statement in the article that is about Arcteon Digital or its services or products,
        and every specific number, statistic, price, timeline, named client, named standard, law, study,
        award or certification. A statement is UNSUPPORTED if the FACT SHEET does not state it.
        General advice that is not about Arcteon and contains no specific figures or named sources is acceptable.
        Output one JSON object and nothing else:
        {"verdict":"pass" or "fail","unsupported":["short quote or description of each unsupported statement"]}
        Use "pass" only when the unsupported list is empty.
        """;

    public async Task<BlogPost> GenerateAsync(string keyword, CancellationToken ct)
    {
        var settings = options.Value;
        var factSheet = facts.Build();
        var existing = store.All();

        // 1. Writer
        var existingTitles = string.Join("\n", existing.Select(p => "- " + p.Title));
        var writerPrompt = $$"""
            FACT SHEET
            {{factSheet}}

            TASK
            Write an article that targets the search phrase: "{{keyword}}"
            Existing articles (do not repeat their angle):
            {{(existingTitles.Length == 0 ? "(none yet)" : existingTitles)}}

            Requirements
            - At least {{settings.MinWords}} words across the sections.
            - Title under 60 characters that contains the search phrase or a close variant.
            - description: a 120 to 155 character summary for search results.
            - 4 to 6 sections, each with a heading and one to three paragraphs. A section may also have bullets.
              Bullets are shown after all of that section's paragraphs, so introduce them in the section's LAST paragraph
              (for example "...three steps matter most:") and put no paragraph after that introduction.
            - The final section is "How Arcteon Digital can help" and relates the topic to the matching services
              in the FACT SHEET, using only what it says.
            - 3 or 4 FAQs that people searching this phrase might ask.

            JSON shape
            {"title":"","description":"","sections":[{"heading":"","paragraphs":[""],"bullets":[""]}],"faqs":[{"question":"","answer":""}]}
            """;
        var draft = await model.CompleteJsonAsync<BlogPost>(WriterSystem, writerPrompt, ct);
        draft.Keyword = keyword;
        draft.Slug = store.UniqueSlug(draft.Title);
        draft.CreatedUtc = DateTime.UtcNow;
        Clean(draft);

        // 2. Quality gate (plain code, no AI)
        var issues = QualityIssues(draft, keyword, settings.MinWords, existing);

        // 3. Fact-check (second model pass). Skipped when the quality gate already failed.
        if (issues.Count == 0)
        {
            var article = new StringBuilder().AppendLine(draft.Title).AppendLine(draft.Description);
            foreach (var line in draft.AllText()) article.AppendLine(line);
            var checkerPrompt = $"FACT SHEET\n{factSheet}\n\nARTICLE\n{article}";
            FactVerdict? verdict = null;
            try
            {
                verdict = await model.CompleteJsonAsync<FactVerdict>(CheckerSystem, checkerPrompt, ct);
            }
            catch (InvalidOperationException ex)
            {
                // Keep the article (the writer call is the slow, expensive one) and hold it for a manual check.
                issues.Add($"Fact-check could not run ({ex.Message}). Check every claim by hand before publishing.");
            }
            if (verdict is not null &&
                (!string.Equals(verdict.Verdict, "pass", StringComparison.OrdinalIgnoreCase) || verdict.Unsupported.Count > 0))
            {
                issues.Add("Fact-check failed: the article makes statements the fact sheet does not support.");
                issues.AddRange(verdict.Unsupported.Select(u => "Unsupported: " + u));
            }
        }

        // 4. Publish gate
        draft.Issues = issues;
        if (issues.Count == 0 && settings.AutoPublish)
        {
            draft.Status = PostStatus.Published;
            draft.PublishedUtc = DateTime.UtcNow;
        }
        else
        {
            draft.Status = PostStatus.Held;
        }
        store.Save(draft);
        logger.LogInformation("SEO post '{Title}' saved as {Status} ({IssueCount} issues).", draft.Title, draft.Status, issues.Count);
        await NotifyAsync(draft, ct);
        return draft;
    }

    private static void Clean(BlogPost post)
    {
        post.Title = post.Title.Trim();
        post.Description = post.Description.Trim();
        post.Sections = post.Sections.Where(s => !string.IsNullOrWhiteSpace(s.Heading))
            .Select(s => new BlogSection
            {
                Heading = s.Heading.Trim(),
                Paragraphs = s.Paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList(),
                Bullets = s.Bullets.Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.Trim()).ToList(),
            }).ToList();
        post.Faqs = post.Faqs.Where(f => !string.IsNullOrWhiteSpace(f.Question) && !string.IsNullOrWhiteSpace(f.Answer)).ToList();
    }

    private static List<string> QualityIssues(BlogPost post, string keyword, int minWords, IReadOnlyList<BlogPost> existing)
    {
        var issues = new List<string>();
        if (post.Title.Length is < 20 or > 65) issues.Add($"Title length is {post.Title.Length} characters (needs 20 to 65).");
        if (post.Description.Length is < 70 or > 165) issues.Add($"Description length is {post.Description.Length} characters (needs 70 to 165).");
        if (post.Sections.Count < 3) issues.Add("Fewer than 3 sections.");
        if (post.Faqs.Count < 2) issues.Add("Fewer than 2 FAQs.");
        if (post.WordCount() < minWords) issues.Add($"Only {post.WordCount()} words (needs {minWords}).");

        var tokens = keyword.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length > 2).ToArray();
        var head = (post.Title + " " + post.Description).ToLowerInvariant();
        if (tokens.Length > 0 && tokens.Count(head.Contains) < Math.Max(1, tokens.Length * 2 / 3))
            issues.Add("The title and description do not contain the target keyword.");

        var all = string.Join("\n", post.AllText().Append(post.Title).Append(post.Description));
        if (Banned.IsMatch(all) || PromisesGuarantee(all))
            issues.Add("Contains a banned promotional phrase (guarantee, #1, best in Malaysia, 100%).");
        if (Markup.IsMatch(all)) issues.Add("Contains HTML or markdown markup.");

        var mine = Shingles(string.Join(" ", post.AllText()));
        foreach (var other in existing)
        {
            var theirs = Shingles(string.Join(" ", other.AllText()));
            var union = mine.Union(theirs).Count();
            if (union > 0 && (double)mine.Intersect(theirs).Count() / union > 0.3)
            {
                issues.Add($"Too similar to the existing post '{other.Title}'.");
                break;
            }
        }
        return issues;
    }

    private static HashSet<string> Shingles(string text)
    {
        var words = text.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var set = new HashSet<string>();
        for (var i = 0; i + 5 <= words.Length; i++) set.Add(string.Join(' ', words[i..(i + 5)]));
        return set;
    }

    private async Task NotifyAsync(BlogPost post, CancellationToken ct)
    {
        if (!options.Value.EmailNotifications) return;
        try
        {
            if (!MailboxAddress.TryParse(smtpOptions.Value.FromAddress, out var from)) return;
            var baseUrl = (config["Site:BaseUrl"] ?? "").TrimEnd('/');
            var published = post.Status == PostStatus.Published;
            var body = new BodyBuilder
            {
                TextBody = published
                    ? $"Published automatically: {post.Title}\nKeyword: {post.Keyword}\nPath: /blog/{post.Slug}\n\nIf anything is wrong, unpublish it at /admin/seo."
                    : $"A draft was held for review: {post.Title}\nKeyword: {post.Keyword}\n\nReasons:\n- {string.Join("\n- ", post.Issues.DefaultIfEmpty("Auto-publish is off."))}\n\nReview it at {baseUrl}/admin/seo/posts/{post.Slug}"
            };
            using var scope = scopes.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<SmtpSender>();
            await sender.SendAsync((published ? "[SEO] Published: " : "[SEO] Needs review: ") + post.Title, from, body, career: false, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("SEO notification email failed: {Error}", ex.Message);
        }
    }

    /// <summary>"Guarantee" is fine inside a disclaimer ("no approach guarantees outcomes") but not as a promise.</summary>
    private static bool PromisesGuarantee(string text)
    {
        foreach (Match match in Guarantee.Matches(text))
        {
            var before = text[Math.Max(0, match.Index - 40)..match.Index];
            if (!Negation.IsMatch(before)) return true;
        }
        return false;
    }

    private static readonly Regex Banned = new(@"\b(number one|best in malaysia|top-ranked)\b|#1|100\s?%", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Guarantee = new(@"\bguarantee[sd]?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Negation = new(@"\b(no|not|never|cannot|without|nor)\b|n't", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Markup = new(@"</?[a-z][^>]*>|```|^\s*#{1,6}\s|\*\*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private sealed class FactVerdict
    {
        public string Verdict { get; set; } = "fail";
        public List<string> Unsupported { get; set; } = [];
    }
}
