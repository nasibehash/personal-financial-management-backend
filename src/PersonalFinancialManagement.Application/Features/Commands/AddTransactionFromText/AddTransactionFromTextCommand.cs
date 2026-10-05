using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromText;

// Records a transaction from a sentence such as "دیروز ناهار ۲۵۰ هزار تومان". With Preview = true
// nothing is saved and the interpreted draft is returned so it can be confirmed first.
public record AddTransactionFromTextCommand(string Text, Guid? AccountId = null, bool Preview = false)
    : IRequest<AiTransactionResultDto>;
