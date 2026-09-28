using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Application.Features.Health;

namespace PersonalFinancialManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ISender _sender;

    public HealthController(ISender sender) => _sender = sender;

    // GET api/health — quick check that the API is running
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetHealthQuery(), cancellationToken));
}
