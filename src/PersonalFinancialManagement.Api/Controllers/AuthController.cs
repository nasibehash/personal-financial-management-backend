using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.LoginUser;
using PersonalFinancialManagement.Application.Features.Commands.RegisterUser;
using PersonalFinancialManagement.Application.Features.Queries.GetCurrentUser;

namespace PersonalFinancialManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    // POST api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(command, cancellationToken);
        return Created("/api/auth/me", response);
    }

    // POST api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginUserCommand command, CancellationToken cancellationToken)
        => Ok(await _sender.Send(command, cancellationToken));

    // GET api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetCurrentUserQuery(), cancellationToken));
}
