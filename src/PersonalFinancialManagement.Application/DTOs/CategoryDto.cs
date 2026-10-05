using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.DTOs;

public record CategoryDto(Guid Id, string Name, CategoryType Type);
