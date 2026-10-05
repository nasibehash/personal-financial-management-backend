using FluentValidation;

namespace PersonalFinancialManagement.Application.Features.Commands.AddTransactionFromVoice;

public class AddTransactionFromVoiceCommandValidator : AbstractValidator<AddTransactionFromVoiceCommand>
{
    // Formats accepted by common speech-to-text services (browsers record webm/ogg, phones m4a/mp3/wav).
    private static readonly string[] AllowedExtensions =
        [".flac", ".m4a", ".mp3", ".mp4", ".mpeg", ".mpga", ".oga", ".ogg", ".wav", ".webm"];

    public AddTransactionFromVoiceCommandValidator()
    {
        RuleFor(x => x.Audio).NotNull();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"The audio format is not supported. Use one of: {string.Join(", ", AllowedExtensions)}.");
    }
}
