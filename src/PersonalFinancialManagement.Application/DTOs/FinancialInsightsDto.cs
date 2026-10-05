namespace PersonalFinancialManagement.Application.DTOs;

// GeneratedByAi is false when the insights came from the built-in rules (no AI provider configured or it failed).
public record FinancialInsightsDto(DateTime From, DateTime To, IReadOnlyList<string> Insights, bool GeneratedByAi);
