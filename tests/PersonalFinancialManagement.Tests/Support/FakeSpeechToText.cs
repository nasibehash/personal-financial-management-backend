using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Tests.Support;

public class FakeSpeechToText : ISpeechToTextService
{
    public string Transcript { get; set; } = string.Empty;

    public Exception? Failure { get; set; }

    public int Calls { get; private set; }

    public string? LastFileName { get; private set; }

    public string? LastContentType { get; private set; }

    public byte[]? LastAudio { get; private set; }

    public async Task<string> TranscribeAsync(Stream audio, string fileName, string contentType, CancellationToken cancellationToken)
    {
        Calls++;
        LastFileName = fileName;
        LastContentType = contentType;

        using var buffer = new MemoryStream();
        await audio.CopyToAsync(buffer, cancellationToken);
        LastAudio = buffer.ToArray();

        return Failure is null ? Transcript : throw Failure;
    }
}
