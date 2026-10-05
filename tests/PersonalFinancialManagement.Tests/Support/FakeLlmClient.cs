using PersonalFinancialManagement.Infrastructure.Services.Ai;

namespace PersonalFinancialManagement.Tests.Support;

// Returns canned replies (or throws) instead of calling a language model, and records what it was asked.
public class FakeLlmClient : ILlmClient
{
    public FakeLlmClient(string reply) => Reply = _ => reply;

    public FakeLlmClient(Func<string, string> reply) => Reply = reply;

    // Maps the user prompt to the reply; tests may replace it.
    public Func<string, string> Reply { get; set; }

    public int Calls { get; private set; }

    public string? LastSystemPrompt { get; private set; }

    public string? LastUserPrompt { get; private set; }

    public Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        Calls++;
        LastSystemPrompt = systemPrompt;
        LastUserPrompt = userPrompt;
        return Task.FromResult(Reply(userPrompt));
    }

    public static FakeLlmClient Failing(Exception exception) => new(_ => throw exception);
}
