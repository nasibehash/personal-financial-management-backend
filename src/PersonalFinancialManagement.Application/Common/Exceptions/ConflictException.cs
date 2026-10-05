namespace PersonalFinancialManagement.Application.Common.Exceptions;

// The operation conflicts with the current state of a resource (maps to 409).
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
