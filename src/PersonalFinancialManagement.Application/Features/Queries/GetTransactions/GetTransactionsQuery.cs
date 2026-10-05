using MediatR;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Queries.GetTransactions;

// From and To are dates (the time is ignored); To is inclusive. AccountId matches
// the source or the destination account of a transfer. Newest transactions come first.
public record GetTransactionsQuery(
    DateTime? From = null,
    DateTime? To = null,
    TransactionType? Type = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TransactionDto>>;
