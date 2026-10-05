namespace PersonalFinancialManagement.Application.DTOs;

public record GoalContributionDto(Guid Id, decimal Amount, DateTime Date, string? Note);
