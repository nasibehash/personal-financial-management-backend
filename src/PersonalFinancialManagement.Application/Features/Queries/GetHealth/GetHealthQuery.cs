using MediatR;

namespace PersonalFinancialManagement.Application.Features.Queries.GetHealth;

// Sample query showing the request/handler pattern.
// Queries live under Features/Queries/<Name>/, commands under Features/Commands/<Name>/.
public record GetHealthQuery : IRequest<HealthDto>;
