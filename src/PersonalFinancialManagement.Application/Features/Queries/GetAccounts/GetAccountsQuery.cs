using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Queries.GetAccounts;

public record GetAccountsQuery(bool IncludeArchived = false) : IRequest<IReadOnlyList<AccountDto>>;
