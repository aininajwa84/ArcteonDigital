using System.Threading.RateLimiting;
using Dewi;
using Dewi.Email;
using Dewi.Seo;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<SmtpSender>();
builder.Services.Configure<SeoSettings>(builder.Configuration.GetSection("Seo"));
builder.Services.AddSingleton<BlogStore>();
builder.Services.AddSingleton<FactSheet>();
// The first real Gemini article took about 2.5 minutes, so allow plenty of time.
builder.Services.AddHttpClient<GeminiText>(c => c.Timeout = TimeSpan.FromMinutes(10));
builder.Services.AddHttpClient<GroqText>(c => c.Timeout = TimeSpan.FromMinutes(10));
builder.Services.AddSingleton<ClaudeText>();
builder.Services.AddSingleton<ITextModel>(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SeoSettings>>().Value.Provider.Trim().ToLowerInvariant() switch
    {
        "claude" => sp.GetRequiredService<ClaudeText>(),
        "groq" => sp.GetRequiredService<GroqText>(),
        _ => sp.GetRequiredService<GeminiText>()
    });
builder.Services.AddSingleton<ContentPipeline>();
builder.Services.AddSingleton<SeoAgent>();
builder.Services.AddHostedService<SeoWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("website-forms", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1)
        }));
});
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseResponseCompression();
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".apk"] = "application/vnd.android.package-archive";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = ctx =>
    {
        // Files are fingerprinted with asp-append-version, so a week of caching is safe.
        ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});
app.UseRouting();
app.UseRateLimiter();
app.MapRazorPages();
app.MapSeoEndpoints();
app.MapSeoAdmin();
app.MapWebsiteForms();
app.Run();
