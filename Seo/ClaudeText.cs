using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;

namespace Dewi.Seo;

/// <summary>Claude through the official Anthropic SDK. Paid; select with Seo:Provider = "Claude".</summary>
public sealed class ClaudeText(IOptions<SeoSettings> options) : ITextModel
{
    private readonly SeoSettings _settings = options.Value;
    private AnthropicClient? _client;

    private string Model => string.IsNullOrWhiteSpace(_settings.Model) ? "claude-sonnet-5-5" : _settings.Model;

    public string Name => "Claude " + Model;

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(_settings.ApiKey) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));

    private AnthropicClient Client => _client ??= string.IsNullOrWhiteSpace(_settings.ApiKey)
        ? new AnthropicClient()
        : new AnthropicClient { ApiKey = _settings.ApiKey };

    public async Task<string> CompleteAsync(string system, string user, bool json, CancellationToken ct)
    {
        var response = await Client.Messages.Create(new MessageCreateParams
        {
            Model = Model,
            MaxTokens = 16000,
            System = system,
            Messages = [new UserMessageParam { Content = user }],
            OutputConfig = new OutputConfig { Effort = Effort.Medium },
        }, ct);

        if (response.StopReason == "refusal")
            throw new InvalidOperationException("The model declined this request.");
        if (response.StopReason == "max_tokens")
            throw new InvalidOperationException("The model response was cut off.");

        var sb = new StringBuilder();
        foreach (var text in response.Content.Select(b => b.Value).OfType<TextBlock>())
            sb.Append(text.Text);
        return sb.ToString();
    }
}
