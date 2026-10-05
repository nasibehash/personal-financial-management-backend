namespace PersonalFinancialManagement.Application.Common.Models;

public record AuthToken(string Token, DateTime ExpiresAtUtc);
