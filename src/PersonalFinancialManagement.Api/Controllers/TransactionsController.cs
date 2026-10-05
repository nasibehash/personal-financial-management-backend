using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Api.Contracts;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.CreateTransaction;
using PersonalFinancialManagement.Application.Features.Commands.DeleteTransaction;
using PersonalFinancialManagement.Application.Features.Commands.UpdateTransaction;
using PersonalFinancialManagement.Application.Features.Queries.GetTransactionById;
using PersonalFinancialManagement.Application.Features.Queries.GetTransactions;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public TransactionsController(ISender sender) => _sender = sender;

    // GET api/transactions?from=2026-10-01&to=2026-10-31&type=Expense&accountId=...&categoryId=...&search=...&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] TransactionType? type,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _sender.Send(
            new GetTransactionsQuery(from, to, type, accountId, categoryId, search, page, pageSize),
            cancellationToken));

    // GET api/transactions/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetTransactionByIdQuery(id), cancellationToken));

    // POST api/transactions
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(TransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await _sender.Send(
            new CreateTransactionCommand(
                request.Type,
                request.Amount,
                request.AccountId,
                request.CategoryId,
                request.DestinationAccountId,
                request.Date,
                request.Description),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction);
    }

    // PUT api/transactions/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> Update(Guid id, TransactionRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(
            new UpdateTransactionCommand(
                id,
                request.Type,
                request.Amount,
                request.AccountId,
                request.CategoryId,
                request.DestinationAccountId,
                request.Date,
                request.Description),
            cancellationToken));

    // DELETE api/transactions/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteTransactionCommand(id), cancellationToken);
        return NoContent();
    }
}
