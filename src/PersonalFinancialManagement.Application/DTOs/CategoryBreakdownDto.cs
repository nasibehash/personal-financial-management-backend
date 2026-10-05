using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.DTOs;

public record CategorySpendingDto(Guid CategoryId, string CategoryName, decimal Total, decimal Percentage, int TransactionCount);

// Categories are ordered from the largest total to the smallest. Percentage is the share of Total.
public record CategoryBreakdownDto(
    DateTime From,
    DateTime To,
    CategoryType Type,
    decimal Total,
    IReadOnlyList<CategorySpendingDto> Categories);
