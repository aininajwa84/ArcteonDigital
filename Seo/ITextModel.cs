using System.Text.Json;

namespace Dewi.Seo;

/// <summary>Any text-generation provider (Gemini, Groq, Claude). Prompt in, text out.</summary>
public interface ITextModel
{
    /// <summary>Provider and model name, for display only.</summary>
    string Name { get; }

    bool HasCredentials { get; }

    /// <param name="json">True when the reply must be a JSON object (the provider's JSON mode is used where available).</param>
    Task<string> CompleteAsync(string system, string user, bool json, CancellationToken ct);
}

/// <summary>The provider is overloaded or rate-limited (HTTP 429 or 5xx). Worth retrying after a wait.</summary>
public sealed class ModelBusyException(string message) : InvalidOperationException(message);

public static class TextModelExtensions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const int MaxAttempts = 4;

    /// <summary>Asks for a JSON object. Retries busy errors up to 4 times, waiting 30, 60 then 90 seconds.</summary>
    public static async Task<T> CompleteJsonAsync<T>(this ITextModel model, string system, string user, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CompleteJsonOnceAsync<T>(model, system, user, ct);
            }
            catch (ModelBusyException) when (attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(30 * attempt), ct);
            }
        }
    }

    private static async Task<T> CompleteJsonOnceAsync<T>(ITextModel model, string system, string user, CancellationToken ct)
    {
        var text = await model.CompleteAsync(system, user, json: true, ct);
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) throw new InvalidOperationException("The model did not return JSON.");
        try
        {
            return JsonSerializer.Deserialize<T>(text[start..(end + 1)], Json)
                ?? throw new InvalidOperationException("The model returned empty JSON.");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("The model returned JSON in an unexpected shape.");
        }
    }
}
