using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Application.Interfaces;

public interface IJwtTokenGenerator
{
    AuthToken Generate(User user);
}
