using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Api.Contracts;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.AddGoalContribution;
using PersonalFinancialManagement.Application.Features.Commands.CreateGoal;
using PersonalFinancialManagement.Application.Features.Commands.DeleteGoal;
using PersonalFinancialManagement.Application.Features.Commands.DeleteGoalContribution;
using PersonalFinancialManagement.Application.Features.Commands.UpdateGoal;
using PersonalFinancialManagement.Application.Features.Queries.GetGoalById;
using PersonalFinancialManagement.Application.Features.Queries.GetGoals;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/goals")]
public class GoalsController : ControllerBase
{
    private readonly ISender _sender;

    public GoalsController(ISender sender) => _sender = sender;

    // GET api/goals?status=Active
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoalDto>>> GetAll([FromQuery] GoalStatus? status, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetGoalsQuery(status), cancellationToken));

    // GET api/goals/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoalDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetGoalByIdQuery(id), cancellationToken));

    // POST api/goals
    [HttpPost]
    public async Task<ActionResult<GoalDto>> Create(CreateGoalCommand command, CancellationToken cancellationToken)
    {
        var goal = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
    }

    // PUT api/goals/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GoalDto>> Update(Guid id, UpdateGoalRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(
            new UpdateGoalCommand(id, request.Name, request.TargetAmount, request.Deadline, request.Description, request.IsCancelled),
            cancellationToken));

    // DELETE api/goals/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteGoalCommand(id), cancellationToken);
        return NoContent();
    }

    // POST api/goals/{id}/contributions
    [HttpPost("{id:guid}/contributions")]
    public async Task<ActionResult<GoalDto>> AddContribution(Guid id, AddGoalContributionRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new AddGoalContributionCommand(id, request.Amount, request.Date, request.Note), cancellationToken));

    // DELETE api/goals/{id}/contributions/{contributionId}
    [HttpDelete("{id:guid}/contributions/{contributionId:guid}")]
    public async Task<ActionResult<GoalDto>> DeleteContribution(Guid id, Guid contributionId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new DeleteGoalContributionCommand(id, contributionId), cancellationToken));
}
