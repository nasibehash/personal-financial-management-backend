using MediatR;
using PersonalFinancialManagement.Application.DTOs;

namespace PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromVoice;

// Records a transaction from a voice recording: the audio is transcribed and the transcript is
// handled like text input. With Preview = true nothing is saved.
public record AddTransactionFromVoiceCommand(
    Stream Audio,
    string FileName,
    string ContentType,
    Guid? AccountId = null,
    bool Preview = false) : IRequest<AiTransactionResultDto>;
