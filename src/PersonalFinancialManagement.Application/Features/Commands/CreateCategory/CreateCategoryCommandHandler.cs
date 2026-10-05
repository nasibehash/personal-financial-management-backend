using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var name = request.Name.Trim();

        var exists = await _db.Categories.AnyAsync(
            c => c.UserId == userId && c.Type == request.Type && c.Name == name, cancellationToken);
        if (exists)
            throw new ConflictException($"A {request.Type.ToString().ToLowerInvariant()} category named '{name}' already exists.");

        var category = new Category { UserId = userId, Name = name, Type = request.Type };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.Type);
    }
}
