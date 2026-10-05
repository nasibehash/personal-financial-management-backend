namespace PersonalFinancialManagement.Application.Interfaces;

// Gives handlers access to the authenticated caller without depending on ASP.NET Core.
public interface ICurrentUserService
{
    // Throws UnauthorizedException when there is no authenticated user.
    Guid UserId { get; }
}
