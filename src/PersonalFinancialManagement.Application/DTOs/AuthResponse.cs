namespace PersonalFinancialManagement.Application.DTOs;

public record AuthResponse(string Token, DateTime ExpiresAtUtc, UserDto User);
