using System.Text.Json;
using PersonalFinancialManagement.Application.Common.Models;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

// Turns a financial snapshot into short Persian insights with a language model.
public class LlmFinancialInsightGenerator
{
    private const string SystemPrompt = """
        You are a concise personal finance coach for a Persian-speaking user.
        Using ONLY the JSON data provided, write 3 to 5 short, specific and actionable insights in Persian.
        Look at income versus expenses, the savings rate, the biggest spending categories, the change versus the
        previous period and the progress of the goals. Quote numbers exactly as given and never invent figures.
        Do not give investment or legal advice.
        Reply with ONLY a JSON object: {"insights": ["...", "..."]}
        """;

    private readonly ILlmClient _llm;

    public LlmFinancialInsightGenerator(ILlmClient llm) => _llm = llm;

    public async Task<IReadOnlyList<string>> GenerateAsync(FinancialSnapshot snapshot, CancellationToken cancellationToken)
    {
        var reply = await _llm.CompleteJsonAsync(SystemPrompt, JsonSerializer.Serialize(snapshot), cancellationToken);

        using var document = JsonDocument.Parse(reply);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("insights", out var insights)
            || insights.ValueKind != JsonValueKind.Array)
            throw new JsonException("The language model reply does not contain an 'insights' array.");

        var items = insights.EnumerateArray()
            .Where(i => i.ValueKind == JsonValueKind.String)
            .Select(i => i.GetString()!.Trim())
            .Where(i => i.Length > 0)
            .Take(5)
            .ToList();

        return items.Count > 0 ? items : throw new JsonException("The language model returned no insights.");
    }
}
