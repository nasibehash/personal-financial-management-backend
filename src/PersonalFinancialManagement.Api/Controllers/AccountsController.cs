using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Api.Contracts;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.CreateAccount;
using PersonalFinancialManagement.Application.Features.Commands.DeleteAccount;
using PersonalFinancialManagement.Application.Features.Commands.UpdateAccount;
using PersonalFinancialManagement.Application.Features.Queries.GetAccountById;
using PersonalFinancialManagement.Application.Features.Queries.GetAccounts;

namespace PersonalFinancialManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly ISender _sender;

    public AccountsController(ISender sender) => _sender = sender;

    // GET api/accounts?includeArchived=false
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAll([FromQuery] bool includeArchived, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetAccountsQuery(includeArchived), cancellationToken));

    // GET api/accounts/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetAccountByIdQuery(id), cancellationToken));

    // POST api/accounts
    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    // PUT api/accounts/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountDto>> Update(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(
            new UpdateAccountCommand(id, request.Name, request.Type, request.InitialBalance, request.IsArchived),
            cancellationToken));

    // DELETE api/accounts/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteAccountCommand(id), cancellationToken);
        return NoContent();
    }
}
