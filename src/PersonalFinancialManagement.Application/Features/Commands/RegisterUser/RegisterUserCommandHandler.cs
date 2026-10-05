using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Application.Features.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RegisterUserCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };
        _db.Users.Add(user);

        foreach (var (name, type) in DefaultCategories.All)
            _db.Categories.Add(new Category { UserId = user.Id, Name = name, Type = type });

        await _db.SaveChangesAsync(cancellationToken);

        var token = _tokenGenerator.Generate(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, new UserDto(user.Id, user.FullName, user.Email));
    }
}
