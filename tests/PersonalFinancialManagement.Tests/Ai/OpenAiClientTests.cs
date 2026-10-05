using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Infrastructure.Options;
using PersonalFinancialManagement.Infrastructure.Services.Ai;
using PersonalFinancialManagement.Tests.Support;

namespace PersonalFinancialManagement.Tests.Ai;

public class OpenAiClientTests
{
    private static readonly AiOptions Configured = new()
    {
        ApiKey = "test-key",
        ChatModel = "chat-model",
        TranscriptionModel = "stt-model",
        Language = "fa"
    };

    private static HttpClient Http(StubHttpMessageHandler handler)
        => new(handler) { BaseAddress = new Uri("https://ai.example.test/v1/") };

    [Fact]
    public async Task Chat_posts_the_prompts_and_returns_the_reply_text()
    {
        var handler = new StubHttpMessageHandler("""{"choices":[{"message":{"role":"assistant","content":"{\"ok\":true}"}}]}""");
        var client = new OpenAiChatClient(Http(handler), Options.Create(Configured));

        var reply = await client.CompleteJsonAsync("system text", "user text", default);

        Assert.Equal("""{"ok":true}""", reply);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://ai.example.test/v1/chat/completions", handler.Request.RequestUri!.ToString());

        using var body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("chat-model", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("json_object", body.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        var messages = body.RootElement.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("system text", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("user text", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Chat_reports_a_failed_request_as_an_external_service_error()
    {
        var handler = new StubHttpMessageHandler("""{"error":"nope"}""", HttpStatusCode.Unauthorized);
        var client = new OpenAiChatClient(Http(handler), Options.Create(Configured));

        var error = await Assert.ThrowsAsync<ExternalServiceException>(() => client.CompleteJsonAsync("s", "u", default));

        Assert.Contains("401", error.Message);
    }

    [Fact]
    public async Task Chat_treats_an_empty_reply_as_an_error()
    {
        var handler = new StubHttpMessageHandler("""{"choices":[{"message":{"content":""}}]}""");
        var client = new OpenAiChatClient(Http(handler), Options.Create(Configured));

        await Assert.ThrowsAsync<ExternalServiceException>(() => client.CompleteJsonAsync("s", "u", default));
    }

    [Fact]
    public async Task Chat_refuses_to_run_without_an_api_key()
    {
        var handler = new StubHttpMessageHandler("{}");
        var client = new OpenAiChatClient(Http(handler), Options.Create(new AiOptions()));

        await Assert.ThrowsAsync<ExternalServiceException>(() => client.CompleteJsonAsync("s", "u", default));
        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task Transcription_uploads_the_audio_and_returns_the_text()
    {
        var handler = new StubHttpMessageHandler("""{"text":"  خرید نان بیست هزار تومان "}""");
        var service = new OpenAiSpeechToTextService(Http(handler), Options.Create(Configured));
        await using var audio = new MemoryStream([1, 2, 3, 4]);

        var text = await service.TranscribeAsync(audio, "note.webm", "audio/webm", default);

        Assert.Equal("خرید نان بیست هزار تومان", text);
        Assert.Equal("https://ai.example.test/v1/audio/transcriptions", handler.Request!.RequestUri!.ToString());
        Assert.StartsWith("multipart/form-data", handler.RequestContentType);
        Assert.Contains("name=model", handler.RequestBody!.Replace("\"", string.Empty));
        Assert.Contains("stt-model", handler.RequestBody);
        Assert.Contains("name=language", handler.RequestBody.Replace("\"", string.Empty));
        Assert.Contains("filename=note.webm", handler.RequestBody.Replace("\"", string.Empty));
        Assert.Contains("audio/webm", handler.RequestBody);
    }

    [Fact]
    public async Task Transcription_returns_an_empty_string_when_there_is_no_text()
    {
        var handler = new StubHttpMessageHandler("{}");
        var service = new OpenAiSpeechToTextService(Http(handler), Options.Create(Configured));
        await using var audio = new MemoryStream([1]);

        Assert.Equal(string.Empty, await service.TranscribeAsync(audio, "a.mp3", "audio/mpeg", default));
    }

    [Fact]
    public async Task Transcription_reports_a_failed_request_as_an_external_service_error()
    {
        var handler = new StubHttpMessageHandler("{}", HttpStatusCode.BadRequest);
        var service = new OpenAiSpeechToTextService(Http(handler), Options.Create(Configured));
        await using var audio = new MemoryStream([1]);

        await Assert.ThrowsAsync<ExternalServiceException>(() => service.TranscribeAsync(audio, "a.mp3", "audio/mpeg", default));
    }

    [Fact]
    public async Task Transcription_refuses_to_run_without_an_api_key()
    {
        var handler = new StubHttpMessageHandler("{}");
        var service = new OpenAiSpeechToTextService(Http(handler), Options.Create(new AiOptions()));
        await using var audio = new MemoryStream([1]);

        await Assert.ThrowsAsync<ExternalServiceException>(() => service.TranscribeAsync(audio, "a.mp3", "audio/mpeg", default));
        Assert.Null(handler.Request);
    }
}
