using MediatR;

namespace PersonalFinancialManagement.Application.Features.Queries.GetHealth;

public class GetHealthQueryHandler : IRequestHandler<GetHealthQuery, HealthDto>
{
    public Task<HealthDto> Handle(GetHealthQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new HealthDto("ok", DateTime.UtcNow));
}
