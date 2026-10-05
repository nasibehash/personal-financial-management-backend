namespace PersonalFinancialManagement.Application.Common.Exceptions;

// A business rule was violated by an otherwise valid request (maps to 400).
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
