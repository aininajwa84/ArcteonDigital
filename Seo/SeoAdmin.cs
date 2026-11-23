using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Dewi.Seo;

/// <summary>Password-protected review page at /admin/seo (HTTP Basic: any user name, password from Seo:AdminPassword).</summary>
public static class SeoAdmin
{
    public static void MapSeoAdmin(this WebApplication app)
    {
        var g = app.MapGroup("/admin/seo");

        g.AddEndpointFilter(async (ctx, next) =>
        {
            var http = ctx.HttpContext;
            http.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            http.Response.Headers.CacheControl = "no-store";
            var password = http.RequestServices.GetRequiredService<IOptions<SeoSettings>>().Value.AdminPassword;
            if (string.IsNullOrEmpty(password)) return Results.NotFound();

            if (!IsAuthorized(http.Request.Headers.Authorization.ToString(), password))
            {
                await Task.Delay(500);
                http.Response.Headers.WWWAuthenticate = "Basic realm=\"SEO admin\", charset=\"UTF-8\"";
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }

            // CSRF guard: browsers resend Basic credentials automatically, so reject cross-site POSTs.
            if (!HttpMethods.IsGet(http.Request.Method) &&
                http.Request.Headers.Origin.ToString() is { Length: > 0 } origin &&
                !Uri.TryCreate(origin, UriKind.Absolute, out var o) | o?.Authority != http.Request.Host.Value)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            return await next(ctx);
        });

        g.MapGet("", (HttpContext http, BlogStore store, IOptions<SeoSettings> o, ITextModel model, SeoAgent agent) =>
            Html(Dashboard(store, o.Value, model, agent, http.Request.Query["msg"].ToString())));

        g.MapGet("/posts/{slug}", (string slug, BlogStore store) =>
            store.Find(slug) is { } post ? Html(Detail(post)) : Results.NotFound());

        g.MapPost("/run", async (HttpContext http, SeoAgent agent, IHostApplicationLifetime life) =>
        {
            var form = await http.Request.ReadFormAsync();
            var keyword = form["keyword"].ToString();
            // Not wrapped in Task.Run: the run takes its lock before the first await, so IsRunning is already true when the page reloads.
            _ = agent.RunOnceAsync(manual: true, life.ApplicationStopping, keyword);
            return Results.Redirect("/admin/seo?msg=" + Uri.EscapeDataString("Run started. This page refreshes itself until the article is ready."));
        });

        g.MapPost("/keywords", async (HttpContext http, BlogStore store) =>
        {
            var form = await http.Request.ReadFormAsync();
            var lines = form["keywords"].ToString().Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries);
            var added = store.AddKeywords(lines);
            return Results.Redirect("/admin/seo?msg=" + Uri.EscapeDataString(added == 0 ? "No new keywords added." : $"Added {added} keyword(s)."));
        });

        g.MapPost("/keywords/remove", async (HttpContext http, BlogStore store) =>
        {
            var form = await http.Request.ReadFormAsync();
            store.RemoveKeyword(form["keyword"].ToString());
            return Results.Redirect("/admin/seo?msg=Keyword%20removed");
        });

        g.MapPost("/pause", (BlogStore store) => { store.IsPaused = true; return Results.Redirect("/admin/seo?msg=Paused"); });
        g.MapPost("/resume", (BlogStore store) => { store.IsPaused = false; return Results.Redirect("/admin/seo?msg=Resumed"); });

        g.MapPost("/posts/{slug}/publish", (string slug, BlogStore store) =>
        {
            if (store.Find(slug) is not { } post) return Results.NotFound();
            post.Status = PostStatus.Published;
            post.PublishedUtc ??= DateTime.UtcNow;
            store.Save(post);
            return Results.Redirect("/admin/seo?msg=Published");
        });

        g.MapPost("/posts/{slug}/unpublish", (string slug, BlogStore store) =>
        {
            if (store.Find(slug) is not { } post) return Results.NotFound();
            post.Status = PostStatus.Held;
            store.Save(post);
            return Results.Redirect("/admin/seo?msg=Unpublished");
        });

        g.MapPost("/posts/{slug}/delete", (string slug, BlogStore store) =>
        {
            store.Delete(slug);
            return Results.Redirect("/admin/seo?msg=Deleted");
        });
    }

    private static bool IsAuthorized(string header, string password)
    {
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header[6..].Trim()));
            var supplied = decoded[(decoded.IndexOf(':') + 1)..];
            return CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(supplied)),
                SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static IResult Html(string body) => Results.Content(
        "<!doctype html><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'>" +
        "<meta name=robots content='noindex,nofollow'><title>SEO admin</title>" +
        "<style>body{font:15px/1.5 system-ui,sans-serif;max-width:900px;margin:2rem auto;padding:0 1rem;color:#1a1a1a}" +
        "table{border-collapse:collapse;width:100%}td,th{border-bottom:1px solid #ddd;padding:.5rem;text-align:left;vertical-align:top}" +
        "button{padding:.3rem .7rem;margin:0 .2rem .2rem 0;cursor:pointer}.held{color:#b45309}.pub{color:#15803d}" +
        ".msg{background:#eef6ff;padding:.6rem 1rem;border-radius:6px}form{display:inline}button:disabled{cursor:wait;opacity:.6}" +
        ".spin{display:inline-block;width:.9em;height:.9em;margin-right:.6em;border:2px solid #93c5fd;border-top-color:#1d4ed8;border-radius:50%;animation:s 1s linear infinite;vertical-align:-.1em}@keyframes s{to{transform:rotate(360deg)}}</style>" +
        "<script>document.addEventListener('submit',e=>{const b=e.target.querySelector('button');if(b){setTimeout(()=>{b.disabled=true;b.textContent='Please wait...'},0)}})</script>" + body,
        "text/html; charset=utf-8");

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static string Dashboard(BlogStore store, SeoSettings s, ITextModel model, SeoAgent agent, string msg)
    {
        var sb = new StringBuilder("<h1>SEO content worker</h1>");
        if (msg.Length > 0) sb.Append($"<p class=msg>{E(msg)}</p>");
        sb.Append($"<p>Status: <b>{(!s.Enabled ? "disabled in settings" : store.IsPaused ? "paused" : "running")}</b> &middot; {E(model.Name)}{(model.HasCredentials ? "" : " <b>(no API key set)</b>")} &middot; " +
                  $"max {s.MaxPostsPerWeek}/week &middot; auto-publish {(s.AutoPublish ? "on" : "off")}</p>");
        if (agent.IsRunning) sb.Append("<meta http-equiv=refresh content=8><script>document.addEventListener('DOMContentLoaded',()=>document.querySelectorAll('form[action$=\"/run\"] button').forEach(b=>{b.disabled=true;b.textContent='Writing...'}))</script>");
        sb.Append(agent.IsRunning
            ? "<p class='msg running'><span class=spin></span><b>Writing an article...</b> This can take a few minutes. The page refreshes by itself every 8 seconds.</p>"
            : agent.LastResult is { } last
                ? $"<p>Last run ({agent.LastResultUtc:yyyy-MM-dd HH:mm} UTC): <b>{E(last)}</b></p>"
                : "<p>No run since the site started.</p>");
        sb.Append("<form method=post action=/admin/seo/run><button>Write next article now</button></form>");
        sb.Append(store.IsPaused
            ? "<form method=post action=/admin/seo/resume><button>Resume worker</button></form>"
            : "<form method=post action=/admin/seo/pause><button>Pause worker (kill switch)</button></form>");
        var used = store.All().Select(p => p.Keyword).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = store.AddedKeywords();
        var pending = added.Concat(s.Keywords).Where(k => !string.IsNullOrWhiteSpace(k) && !used.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        sb.Append($"<p>Next keyword: <b>{E(pending.FirstOrDefault() ?? "none left")}</b></p>");

        sb.Append("<h2>Keywords</h2>");
        sb.Append("<form method=post action=/admin/seo/keywords><p><textarea name=keywords rows=3 cols=60 placeholder='One keyword or phrase per line, e.g. payroll software Malaysia'></textarea></p>" +
                  "<button>Add keywords</button></form>");
        sb.Append("<p><small>The AI writes an article for each keyword using the facts about this website. Waiting to be written:</small></p>");
        if (pending.Count == 0) sb.Append("<p><i>None. Add a keyword above.</i></p>");
        else
        {
            sb.Append("<table>");
            foreach (var k in pending)
            {
                sb.Append($"<tr><td>{E(k)}</td><td><form method=post action=/admin/seo/run><input type=hidden name=keyword value='{E(k)}'><button>Write now</button></form>");
                if (added.Contains(k, StringComparer.OrdinalIgnoreCase))
                    sb.Append($"<form method=post action=/admin/seo/keywords/remove><input type=hidden name=keyword value='{E(k)}'><button>Remove</button></form>");
                sb.Append("</td></tr>");
            }
            sb.Append("</table>");
        }
        sb.Append("<h2>Articles</h2>");
        sb.Append("<table><tr><th>Title</th><th>Status</th><th>Created</th><th></th></tr>");
        foreach (var p in store.All())
        {
            var cls = p.Status == PostStatus.Published ? "pub" : "held";
            sb.Append($"<tr><td><a href='/admin/seo/posts/{E(p.Slug)}'>{E(p.Title)}</a><br><small>{E(p.Keyword)}</small></td>" +
                      $"<td class={cls}>{E(p.Status)}{(p.Issues.Count > 0 ? $" ({p.Issues.Count} issues)" : "")}</td><td>{p.CreatedUtc:yyyy-MM-dd}</td><td>");
            sb.Append(p.Status == PostStatus.Published
                ? $"<form method=post action='/admin/seo/posts/{E(p.Slug)}/unpublish'><button>Unpublish</button></form>"
                : $"<form method=post action='/admin/seo/posts/{E(p.Slug)}/publish'><button>Publish</button></form>");
            sb.Append($"<form method=post action='/admin/seo/posts/{E(p.Slug)}/delete' onsubmit=\"return confirm('Delete permanently?')\"><button>Delete</button></form></td></tr>");
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    private static string Detail(BlogPost p)
    {
        var sb = new StringBuilder($"<p><a href=/admin/seo>&larr; Back</a></p><h1>{E(p.Title)}</h1>");
        sb.Append($"<p><b>{E(p.Status)}</b> &middot; keyword: {E(p.Keyword)} &middot; {p.WordCount()} words</p><p><i>{E(p.Description)}</i></p>");
        if (p.Issues.Count > 0)
            sb.Append("<h2>Held because</h2><ul>" + string.Concat(p.Issues.Select(i => $"<li>{E(i)}</li>")) + "</ul>");
        foreach (var s in p.Sections)
        {
            sb.Append($"<h2>{E(s.Heading)}</h2>");
            foreach (var para in s.Paragraphs) sb.Append($"<p>{E(para)}</p>");
            if (s.Bullets.Count > 0) sb.Append("<ul>" + string.Concat(s.Bullets.Select(b => $"<li>{E(b)}</li>")) + "</ul>");
        }
        sb.Append("<h2>FAQ</h2>");
        foreach (var f in p.Faqs) sb.Append($"<p><b>{E(f.Question)}</b><br>{E(f.Answer)}</p>");
        if (p.Status != PostStatus.Published)
            sb.Append($"<form method=post action='/admin/seo/posts/{E(p.Slug)}/publish'><button>Publish</button></form>");
        return sb.ToString();
    }
}
