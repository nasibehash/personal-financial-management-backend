using MediatR;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Services;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromText;

public class AddTransactionFromTextCommandHandler : IRequestHandler<AddTransactionFromTextCommand, AiTransactionResultDto>
{
    private readonly TransactionTextProcessor _processor;

    public AddTransactionFromTextCommandHandler(TransactionTextProcessor processor) => _processor = processor;

    public Task<AiTransactionResultDto> Handle(AddTransactionFromTextCommand request, CancellationToken cancellationToken)
        => _processor.ProcessAsync(
            request.Text.Trim(),
            transcript: null,
            request.AccountId,
            request.Preview,
            TransactionSource.Text,
            cancellationToken);
}
