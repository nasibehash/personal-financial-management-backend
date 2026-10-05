namespace PersonalFinancialManagement.Api.Contracts;

public record AddGoalContributionRequest(decimal Amount, DateTime? Date = null, string? Note = null);
