namespace PersonalFinancialManagement.Application.DTOs;

// Contributions are listed newest first.
public record GoalDetailDto(GoalDto Goal, IReadOnlyList<GoalContributionDto> Contributions);
