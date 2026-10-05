using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Helpers;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Application.Features.Commands.CreateAccount;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var name = request.Name.Trim();

        if (await _db.Accounts.AnyAsync(a => a.UserId == userId && a.Name == name, cancellationToken))
            throw new ConflictException($"An account named '{name}' already exists.");

        var account = new Account
        {
            UserId = userId,
            Name = name,
            Type = request.Type,
            InitialBalance = request.InitialBalance
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        return account.ToDto(account.InitialBalance);
    }
}
