using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Infrastructure.Options;

namespace PersonalFinancialManagement.Infrastructure.Services.Ai;

// Uses the language model when it is configured and falls back to the built-in rules otherwise.
public class FinancialInsightGenerator : IFinancialInsightGenerator
{
    private readonly AiOptions _options;
    private readonly LlmFinancialInsightGenerator _llmGenerator;
    private readonly RuleBasedInsightGenerator _ruleGenerator;
    private readonly ILogger<FinancialInsightGenerator> _logger;

    public FinancialInsightGenerator(
        IOptions<AiOptions> options,
        LlmFinancialInsightGenerator llmGenerator,
        RuleBasedInsightGenerator ruleGenerator,
        ILogger<FinancialInsightGenerator> logger)
    {
        _options = options.Value;
        _llmGenerator = llmGenerator;
        _ruleGenerator = ruleGenerator;
        _logger = logger;
    }

    public async Task<FinancialInsights> GenerateAsync(FinancialSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (_options.IsConfigured)
        {
            try
            {
                return new FinancialInsights(await _llmGenerator.GenerateAsync(snapshot, cancellationToken), GeneratedByAi: true);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "The language model could not generate insights; using the built-in rules instead");
            }
        }

        return new FinancialInsights(_ruleGenerator.Generate(snapshot), GeneratedByAi: false);
    }
}
