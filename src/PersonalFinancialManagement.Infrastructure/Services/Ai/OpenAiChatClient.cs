using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Infrastructure.Options;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

// Talks to any OpenAI-compatible /chat/completions endpoint.
public class OpenAiChatClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public OpenAiChatClient(HttpClient http, IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
            throw new ExternalServiceException("The AI service is not configured. Set 'Ai:ApiKey'.");

        var request = new
        {
            model = _options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" }
        };

        using var response = await _http.PostAsJsonAsync("chat/completions", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException($"The language model request failed with status {(int)response.StatusCode}.");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return string.IsNullOrWhiteSpace(content)
            ? throw new ExternalServiceException("The language model returned an empty response.")
            : content;
    }
}
