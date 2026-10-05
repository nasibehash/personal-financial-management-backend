using PersonalFinancialManagement.Application.Common.Models;

namespace PersonalFinancialManagement.Application.Interfaces;

public interface ITransactionTextParser
{
    // Returns null when the text does not describe a transaction.
    Task<ParsedTransaction?> ParseAsync(string text, TransactionParsingContext context, CancellationToken cancellationToken);
}
