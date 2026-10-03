namespace PersonalFinancialManagement.Application.Interfaces;

// Abstraction over the database used by MediatR handlers.
// Implemented by ApplicationDbContext in the Infrastructure layer.
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
