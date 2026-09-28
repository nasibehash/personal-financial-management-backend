using Microsoft.AspNetCore.Mvc;

namespace SpendWise.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    // GET api/health — quick check that the API is running
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
