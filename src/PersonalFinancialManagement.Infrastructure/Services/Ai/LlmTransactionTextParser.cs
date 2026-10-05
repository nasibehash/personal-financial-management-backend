using System.Globalization;
using System.Text.Json;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

// Extracts a transaction from free text with a language model.
public class LlmTransactionTextParser
{
    private const string SystemPrompt = """
        You extract one financial transaction from a short message written in Persian or English.
        Reply with ONLY a JSON object with these keys:
          "type": "Income", "Expense" or "Transfer" (null if the message is not a transaction)
          "amount": a positive number. Convert Persian digits and words such as "هزار" (x1000) and "میلیون" (x1000000).
                    Do NOT convert between currencies and ignore the words "تومان" and "ریال"; use the number as the user said it.
          "category": exactly one of the provided category names for that type, or null
          "account": exactly one of the provided account names the money is taken from or paid into, or null
          "destinationAccount": for transfers, exactly one of the provided account names the money goes to, or null
          "description": a short description of the transaction in the user's language
          "date": "yyyy-MM-dd", resolving words like today/yesterday ("امروز", "دیروز", "پریروز") from "currentDate", or null if not mentioned
        Use only names that were provided. Never invent data.
        """;

    private readonly ILlmClient _llm;

    public LlmTransactionTextParser(ILlmClient llm) => _llm = llm;

    public async Task<ParsedTransaction?> ParseAsync(string text, TransactionParsingContext context, CancellationToken cancellationToken)
    {
        var userPrompt = JsonSerializer.Serialize(new
        {
            text,
            currentDate = context.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            incomeCategories = context.IncomeCategories,
            expenseCategories = context.ExpenseCategories,
            accounts = context.AccountNames
        });

        var reply = await _llm.CompleteJsonAsync(SystemPrompt, userPrompt, cancellationToken);
        return ParseReply(reply, context);
    }

    private static ParsedTransaction? ParseReply(string reply, TransactionParsingContext context)
    {
        using var document = JsonDocument.Parse(reply);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("The language model reply is not a JSON object.");

        var typeText = GetString(root, "type");
        if (!Enum.TryParse<TransactionType>(typeText, ignoreCase: true, out var type) || !Enum.IsDefined(type))
            return null;

        if (!TryGetAmount(root, out var amount) || amount <= 0)
            return null;

        var categories = type switch
        {
            TransactionType.Income => context.IncomeCategories,
            TransactionType.Expense => context.ExpenseCategories,
            _ => Array.Empty<string>()
        };

        DateTime? date = null;
        if (DateTime.TryParse(GetString(root, "date"), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
            date = parsedDate;

        var description = GetString(root, "description")?.Trim();
        if (description is { Length: > 500 })
            description = description[..500];

        return new ParsedTransaction(
            type,
            amount,
            Match(GetString(root, "category"), categories),
            Match(GetString(root, "account"), context.AccountNames),
            type == TransactionType.Transfer ? Match(GetString(root, "destinationAccount"), context.AccountNames) : null,
            string.IsNullOrWhiteSpace(description) ? null : description,
            date,
            Method: "ai");
    }

    private static string? GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool TryGetAmount(JsonElement root, out decimal amount)
    {
        amount = 0;
        if (!root.TryGetProperty("amount", out var property))
            return false;

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.TryGetDecimal(out amount),
            JsonValueKind.String => decimal.TryParse(
                PersianText.Normalize(property.GetString() ?? string.Empty),
                NumberStyles.Number, CultureInfo.InvariantCulture, out amount),
            _ => false
        };
    }

    // The model may only pick from the names it was given; anything else is treated as "no match".
    private static string? Match(string? value, IReadOnlyList<string> names)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : names.FirstOrDefault(n => string.Equals(n, value.Trim(), StringComparison.OrdinalIgnoreCase));
}
