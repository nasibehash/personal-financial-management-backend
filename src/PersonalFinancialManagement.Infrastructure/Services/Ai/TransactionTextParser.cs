using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Infrastructure.Options;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

// Uses the language model when it is configured and falls back to the built-in rules when it
// is not, or when the call fails, so text entry keeps working without an AI provider.
public class TransactionTextParser : ITransactionTextParser
{
    private readonly AiOptions _options;
    private readonly LlmTransactionTextParser _llmParser;
    private readonly RuleBasedTransactionTextParser _ruleParser;
    private readonly ILogger<TransactionTextParser> _logger;

    public TransactionTextParser(
        IOptions<AiOptions> options,
        LlmTransactionTextParser llmParser,
        RuleBasedTransactionTextParser ruleParser,
        ILogger<TransactionTextParser> logger)
    {
        _options = options.Value;
        _llmParser = llmParser;
        _ruleParser = ruleParser;
        _logger = logger;
    }

    public async Task<ParsedTransaction?> ParseAsync(string text, TransactionParsingContext context, CancellationToken cancellationToken)
    {
        if (_options.IsConfigured)
        {
            try
            {
                return await _llmParser.ParseAsync(text, context, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "The language model could not parse the transaction text; using the built-in rules instead");
            }
        }

        return _ruleParser.Parse(text, context);
    }
}
