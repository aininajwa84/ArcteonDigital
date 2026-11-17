using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Dewi.Seo;

/// <summary>Stores posts as one JSON file each so they can be inspected and edited by hand.</summary>
public sealed class BlogStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _dir;
    private readonly object _lock = new();
    private List<BlogPost> _posts = [];

    public BlogStore(IOptions<SeoSettings> options, IWebHostEnvironment env, ILogger<BlogStore> logger)
    {
        var path = options.Value.DataPath;
        _dir = Path.IsPathRooted(path) ? path : Path.Combine(env.ContentRootPath, path);
        Directory.CreateDirectory(_dir);
        foreach (var file in Directory.EnumerateFiles(_dir, "*.json"))
        {
            try
            {
                var post = JsonSerializer.Deserialize<BlogPost>(File.ReadAllText(file), Json);
                if (post is not null && IsValidSlug(post.Slug)) _posts.Add(post);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                logger.LogWarning("Skipping unreadable blog file {File}: {Error}", Path.GetFileName(file), ex.Message);
            }
        }
    }

    private string PausedFlag => Path.Combine(_dir, "paused.flag");

    public bool IsPaused
    {
        get => File.Exists(PausedFlag);
        set
        {
            if (value) File.WriteAllText(PausedFlag, DateTime.UtcNow.ToString("O"));
            else if (File.Exists(PausedFlag)) File.Delete(PausedFlag);
        }
    }

    public IReadOnlyList<BlogPost> All()
    {
        lock (_lock) return _posts.OrderByDescending(p => p.CreatedUtc).ToList();
    }

    public IReadOnlyList<BlogPost> Published() =>
        All().Where(p => p.Status == PostStatus.Published).OrderByDescending(p => p.PublishedUtc ?? p.CreatedUtc).ToList();

    public BlogPost? Find(string slug)
    {
        if (!IsValidSlug(slug)) return null;
        lock (_lock) return _posts.FirstOrDefault(p => p.Slug == slug);
    }

    public BlogPost? FindPublished(string slug) =>
        Find(slug) is { Status: PostStatus.Published } post ? post : null;

    public void Save(BlogPost post)
    {
        if (!IsValidSlug(post.Slug)) throw new ArgumentException("Invalid slug.", nameof(post));
        lock (_lock)
        {
            File.WriteAllText(Path.Combine(_dir, post.Slug + ".json"), JsonSerializer.Serialize(post, Json));
            var index = _posts.FindIndex(p => p.Slug == post.Slug);
            if (index >= 0) _posts[index] = post; else _posts.Add(post);
        }
    }

    public bool Delete(string slug)
    {
        if (!IsValidSlug(slug)) return false;
        lock (_lock)
        {
            var file = Path.Combine(_dir, slug + ".json");
            if (File.Exists(file)) File.Delete(file);
            return _posts.RemoveAll(p => p.Slug == slug) > 0;
        }
    }

    public string UniqueSlug(string title)
    {
        var slug = SlugCleaner.Replace(title.ToLowerInvariant(), "-").Trim('-');
        if (slug.Length > 80) slug = slug[..80].Trim('-');
        if (slug.Length == 0) slug = "post";
        var candidate = slug;
        for (var n = 2; Find(candidate) is not null; n++) candidate = $"{slug}-{n}";
        return candidate;
    }

    public static bool IsValidSlug(string? slug) =>
        !string.IsNullOrEmpty(slug) && slug.Length <= 100 && ValidSlug.IsMatch(slug);

    private static readonly Regex SlugCleaner = new("[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex ValidSlug = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
}
