using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Application.Interfaces;

// Abstraction over the database used by MediatR handlers.
// Implemented by ApplicationDbContext in the Infrastructure layer.
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Category> Categories { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Goal> Goals { get; }
    DbSet<GoalContribution> GoalContributions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
