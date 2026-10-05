namespace PersonalFinancialManagement.Application.Common.Exceptions;

// An external service (speech-to-text, language model) is unavailable or failed (maps to 503).
public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message)
    {
    }

    public ExternalServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
