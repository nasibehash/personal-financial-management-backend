using PersonalFinancialManagement.Application.Common.Models;

namespace PersonalFinancialManagement.Application.Interfaces;

public interface IFinancialInsightGenerator
{
    Task<FinancialInsights> GenerateAsync(FinancialSnapshot snapshot, CancellationToken cancellationToken);
}
