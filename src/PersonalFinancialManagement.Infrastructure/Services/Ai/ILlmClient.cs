namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

public interface ILlmClient
{
    // Sends a system and a user prompt and returns the model's reply, which is requested to be a JSON object.
    Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
