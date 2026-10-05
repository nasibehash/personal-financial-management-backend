namespace PersonalFinancialManagement.Api.Contracts;

// preview = true returns the interpreted transaction without saving it.
// accountId is optional; without it the account named in the text (or the user's only account) is used.
public record TextTransactionRequest(string Text, Guid? AccountId = null, bool Preview = false);

// Multipart form: the recording in "audio", plus the optional "accountId" and "preview" fields.
public class VoiceTransactionRequest
{
    public IFormFile? Audio { get; set; }

    public Guid? AccountId { get; set; }

    public bool Preview { get; set; }
}
