using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinancialManagement.Api.Contracts;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.CreateCategory;
using PersonalFinancialManagement.Application.Features.Commands.DeleteCategory;
using PersonalFinancialManagement.Application.Features.Commands.UpdateCategory;
using PersonalFinancialManagement.Application.Features.Queries.GetCategories;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender) => _sender = sender;

    // GET api/categories?type=Expense
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll([FromQuery] CategoryType? type, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetCategoriesQuery(type), cancellationToken));

    // POST api/categories
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _sender.Send(command, cancellationToken);
        return Created($"/api/categories/{category.Id}", category);
    }

    // PUT api/categories/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new UpdateCategoryCommand(id, request.Name), cancellationToken));

    // DELETE api/categories/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
