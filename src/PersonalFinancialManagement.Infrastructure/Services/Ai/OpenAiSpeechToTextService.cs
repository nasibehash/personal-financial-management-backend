using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Infrastructure.Options;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

// Talks to any OpenAI-compatible /audio/transcriptions endpoint.
public class OpenAiSpeechToTextService : ISpeechToTextService
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public OpenAiSpeechToTextService(HttpClient http, IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> TranscribeAsync(Stream audio, string fileName, string contentType, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
            throw new ExternalServiceException("Voice input is not configured. Set 'Ai:ApiKey'.");

        using var form = new MultipartFormDataContent();

        var file = new StreamContent(audio);
        if (MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            file.Headers.ContentType = mediaType;

        form.Add(file, "file", fileName);
        form.Add(new StringContent(_options.TranscriptionModel), "model");
        if (!string.IsNullOrWhiteSpace(_options.Language))
            form.Add(new StringContent(_options.Language), "language");

        using var response = await _http.PostAsync("audio/transcriptions", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException($"The speech-to-text request failed with status {(int)response.StatusCode}.");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return document.RootElement.TryGetProperty("text", out var text)
            ? text.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }
}
