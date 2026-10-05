using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Queries.GetAccountById;

public record GetAccountByIdQuery(Guid Id) : IRequest<AccountDto>;
