namespace PersonalFinancialManagement.Application.Common.Exceptions;

// The caller is not authenticated or the credentials are wrong (maps to 401).
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
