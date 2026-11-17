using Microsoft.Extensions.Options;

namespace Dewi.Seo;

/// <summary>Decides whether to write the next article and runs the pipeline. All safety limits live here.</summary>
public sealed class SeoAgent(
    ContentPipeline pipeline,
    BlogStore store,
    ITextModel model,
    IOptions<SeoSettings> options,
    ILogger<SeoAgent> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>What the most recent run did, shown on the admin page.</summary>
    public string? LastResult { get; private set; }
    public DateTime? LastResultUtc { get; private set; }
    public bool IsRunning => _gate.CurrentCount == 0;

    /// <param name="manual">True when started from the admin page: skips the weekly cap and held-draft limit.</param>
    public async Task<string> RunOnceAsync(bool manual, CancellationToken ct)
    {
        logger.LogInformation("SEO run requested ({Source}).", manual ? "manual" : "scheduled");
        var result = await RunCoreAsync(manual, ct);
        if (!result.StartsWith("Skipped: a run is already", StringComparison.Ordinal))
        {
            LastResult = result;
            LastResultUtc = DateTime.UtcNow;
        }
        logger.LogInformation("SEO run finished: {Result}", result);
        return result;
    }

    private async Task<string> RunCoreAsync(bool manual, CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.Enabled) return "Skipped: Seo:Enabled is false.";
        if (store.IsPaused) return "Skipped: the worker is paused.";
        if (!model.HasCredentials) return $"Skipped: no API key configured for {model.Name}.";
        if (!await _gate.WaitAsync(0, ct)) return "Skipped: a run is already in progress.";
        try
        {
            var all = store.All();
            if (!manual)
            {
                var recent = all.Count(p => p.CreatedUtc > DateTime.UtcNow.AddDays(-7));
                if (recent >= settings.MaxPostsPerWeek) return "Skipped: weekly post limit reached.";
                if (all.Count(p => p.Status == PostStatus.Held) >= settings.MaxHeldDrafts)
                    return "Skipped: too many held drafts waiting for review.";
            }

            var used = all.Select(p => p.Keyword).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var keyword = settings.Keywords.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k) && !used.Contains(k));
            if (keyword is null) return "Skipped: every keyword already has an article. Add more in Seo:Keywords.";

            var post = await pipeline.GenerateAsync(keyword, ct);
            return $"{(post.Status == PostStatus.Published ? "Published" : "Held for review")}: {post.Title}";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return "Cancelled.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SEO content run failed.");
            return "Failed: " + ex.Message;
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed class SeoWorker(SeoAgent agent, IOptions<SeoSettings> options, ILogger<SeoWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await agent.RunOnceAsync(manual: false, stoppingToken);
                logger.LogInformation("SEO worker: {Result}", result);
                var hours = Math.Max(1, options.Value.CheckIntervalHours);
                await Task.Delay(TimeSpan.FromHours(hours), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }
}
