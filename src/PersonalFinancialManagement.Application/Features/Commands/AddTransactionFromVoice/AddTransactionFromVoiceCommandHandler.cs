using MediatR;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Application.Services;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromVoice;

public class AddTransactionFromVoiceCommandHandler : IRequestHandler<AddTransactionFromVoiceCommand, AiTransactionResultDto>
{
    private readonly ISpeechToTextService _speechToText;
    private readonly TransactionTextProcessor _processor;

    public AddTransactionFromVoiceCommandHandler(ISpeechToTextService speechToText, TransactionTextProcessor processor)
    {
        _speechToText = speechToText;
        _processor = processor;
    }

    public async Task<AiTransactionResultDto> Handle(AddTransactionFromVoiceCommand request, CancellationToken cancellationToken)
    {
        var transcript = await _speechToText.TranscribeAsync(request.Audio, request.FileName, request.ContentType, cancellationToken);
        if (string.IsNullOrWhiteSpace(transcript))
            throw new BusinessRuleException("No speech was recognised in the recording.");

        return await _processor.ProcessAsync(
            transcript,
            transcript,
            request.AccountId,
            request.Preview,
            TransactionSource.Voice,
            cancellationToken);
    }
}
