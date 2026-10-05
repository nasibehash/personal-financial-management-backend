using PersonalFinancialManagement.Infrastructure.Services.Ai;

namespace PersonalFinancialManagement.Tests.Support;

// Returns canned replies (or throws) instead of calling a language model, and records what it was asked.
public class FakeLlmClient : ILlmClient
{
    private readonly Func<string, string> _reply;

    public FakeLlmClient(string reply) => _reply = _ => reply;

    public FakeLlmClient(Func<string, string> reply) => _reply = reply;

    public int Calls { get; private set; }

    public string? LastSystemPrompt { get; private set; }

    public string? LastUserPrompt { get; private set; }

    public Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        Calls++;
        LastSystemPrompt = systemPrompt;
        LastUserPrompt = userPrompt;
        return Task.FromResult(_reply(userPrompt));
    }

    public static FakeLlmClient Failing(Exception exception) => new(_ => throw exception);
}
