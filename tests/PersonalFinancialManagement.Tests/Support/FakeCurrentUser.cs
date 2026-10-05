using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Tests.Support;

public class FakeCurrentUser : ICurrentUserService
{
    public Guid? Id { get; set; }

    public Guid UserId => Id ?? throw new UnauthorizedException("Authentication is required.");
}
