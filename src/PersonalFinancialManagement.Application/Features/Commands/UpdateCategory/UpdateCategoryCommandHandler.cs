using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;

namespace PersonalFinancialManagement.Application.Features.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var name = request.Name.Trim();

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == userId, cancellationToken)
                       ?? throw new NotFoundException("Category", request.Id);

        var duplicate = await _db.Categories.AnyAsync(
            c => c.UserId == userId && c.Type == category.Type && c.Name == name && c.Id != category.Id, cancellationToken);
        if (duplicate)
            throw new ConflictException($"A {category.Type.ToString().ToLowerInvariant()} category named '{name}' already exists.");

        category.Name = name;
        await _db.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.Type);
    }
}
