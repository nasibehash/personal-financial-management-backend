namespace PersonalFinancialManagement.Application.Common.Exceptions;

// The requested resource does not exist (maps to 404).
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key) : base($"{entityName} '{key}' was not found.")
    {
    }
}
