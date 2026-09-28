using MediatR;

namespace PersonalFinancialManagement.Application.Features.Health;

// Sample query showing the request/handler pattern. Add new features under Features/<Name>/.
public record GetHealthQuery : IRequest<HealthDto>;

public record HealthDto(string Status, DateTime CheckedAtUtc);

public class GetHealthQueryHandler : IRequestHandler<GetHealthQuery, HealthDto>
{
    public Task<HealthDto> Handle(GetHealthQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new HealthDto("ok", DateTime.UtcNow));
}
