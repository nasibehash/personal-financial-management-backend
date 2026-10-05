namespace PersonalFinancialManagement.Application.Interfaces;

public interface ISpeechToTextService
{
    // Returns the transcript of the recorded audio.
    Task<string> TranscribeAsync(Stream audio, string fileName, string contentType, CancellationToken cancellationToken);
}
