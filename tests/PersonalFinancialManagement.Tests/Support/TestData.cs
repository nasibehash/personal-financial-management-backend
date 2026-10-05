using PersonalFinancialManagement.Domain.Entities;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Tests.Support;

// Helpers for arranging rows directly in the database.
public static class TestData
{
    public static Account Account(Guid userId, string name, decimal initialBalance = 0, bool archived = false)
        => new()
        {
            UserId = userId,
            Name = name,
            Type = AccountType.BankAccount,
            InitialBalance = initialBalance,
            IsArchived = archived
        };

    public static Transaction Transaction(
        Guid userId,
        Guid accountId,
        TransactionType type,
        decimal amount,
        DateTime date,
        Guid? categoryId = null,
        Guid? destinationAccountId = null,
        string? description = null)
        => new()
        {
            UserId = userId,
            AccountId = accountId,
            DestinationAccountId = destinationAccountId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Date = date,
            Description = description
        };
}
