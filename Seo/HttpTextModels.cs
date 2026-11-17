using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Dewi.Seo;

/// <summary>Google Gemini through its REST API. Has a free tier; the key comes from aistudio.google.com.</summary>
public sealed class GeminiText(HttpClient http, IOptions<SeoSettings> options) : ITextModel
{
    private readonly SeoSettings _s = options.Value;
    private string Model => string.IsNullOrWhiteSpace(_s.Model) ? "gemini-2.5-flash" : _s.Model;
    private string Key => !string.IsNullOrWhiteSpace(_s.ApiKey) ? _s.ApiKey : Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";

    public string Name => "Gemini " + Model;
    public bool HasCredentials => !string.IsNullOrWhiteSpace(Key);

    public async Task<string> CompleteAsync(string system, string user, bool json, CancellationToken ct)
    {
        var generation = new Dictionary<string, object> { ["maxOutputTokens"] = 8192 };
        if (json) generation["responseMimeType"] = "application/json";
        var body = new Dictionary<string, object>
        {
            ["systemInstruction"] = new { parts = new[] { new { text = system } } },
            ["contents"] = new[] { new { role = "user", parts = new[] { new { text = user } } } },
            ["generationConfig"] = generation,
        };
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_s.GeminiBaseUrl.TrimEnd('/')}/v1beta/models/{Uri.EscapeDataString(Model)}:generateContent")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", Key);

        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw HttpFailure("Gemini", response, text);

        using var doc = JsonDocument.Parse(text);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini returned no answer (the request may have been blocked).");
        var candidate = candidates[0];
        if (candidate.TryGetProperty("finishReason", out var reason) && reason.GetString() == "MAX_TOKENS")
            throw new InvalidOperationException("The model response was cut off.");
        var sb = new StringBuilder();
        if (candidate.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts))
            foreach (var part in parts.EnumerateArray())
                if (part.TryGetProperty("text", out var t)) sb.Append(t.GetString());
        return sb.ToString();
    }

    internal static Exception HttpFailure(string provider, HttpResponseMessage response, string body)
    {
        // Show the provider's short error message, never the request (which holds the key).
        string detail = "";
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.TryGetProperty("message", out var m))
                detail = m.GetString() ?? "";
        }
        catch (JsonException) { }
        if (detail.Length > 200) detail = detail[..200];
        var message = $"{provider} returned {(int)response.StatusCode}{(response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ? " (free limit reached)" : "")}: {detail}";
        // 429 and 5xx are temporary: the caller retries them after a wait.
        return response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500
            ? new ModelBusyException(message)
            : new InvalidOperationException(message);
    }
}

/// <summary>Groq's OpenAI-compatible API (open models, free tier). Key from console.groq.com.</summary>
public sealed class GroqText(HttpClient http, IOptions<SeoSettings> options) : ITextModel
{
    private readonly SeoSettings _s = options.Value;
    private string Model => string.IsNullOrWhiteSpace(_s.Model) ? "llama-3.3-70b-versatile" : _s.Model;
    private string Key => !string.IsNullOrWhiteSpace(_s.ApiKey) ? _s.ApiKey : Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";

    public string Name => "Groq " + Model;
    public bool HasCredentials => !string.IsNullOrWhiteSpace(Key);

    public async Task<string> CompleteAsync(string system, string user, bool json, CancellationToken ct)
    {
        var body = new Dictionary<string, object>
        {
            ["model"] = Model,
            ["max_tokens"] = 6000,
            ["messages"] = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            },
        };
        if (json) body["response_format"] = new { type = "json_object" };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Key);

        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw GeminiText.HttpFailure("Groq", response, text);

        using var doc = JsonDocument.Parse(text);
        var choice = doc.RootElement.GetProperty("choices")[0];
        if (choice.TryGetProperty("finish_reason", out var reason) && reason.GetString() == "length")
            throw new InvalidOperationException("The model response was cut off.");
        return choice.GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
