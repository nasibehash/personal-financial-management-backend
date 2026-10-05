using MediatR;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Common;
using PersonalFinancialManagement.Application.Common.Exceptions;
using PersonalFinancialManagement.Application.Common.Models;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.CreateTransaction;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;

namespace PersonalFinancialManagement.Application.Services;

// Turns a sentence ("خرید نان ۲۰ هزار تومان") into a transaction draft that fits the user's own
// accounts and categories and, unless it is only a preview, saves it. Used by the text and voice commands.
public class TransactionTextProcessor
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ITransactionTextParser _parser;
    private readonly ISender _sender;

    public TransactionTextProcessor(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ITransactionTextParser parser,
        ISender sender)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _parser = parser;
        _sender = sender;
    }

    // accountId: the account to use; when null the account is taken from the text, or the user's only account.
    public async Task<AiTransactionResultDto> ProcessAsync(
        string text,
        string? transcript,
        Guid? accountId,
        bool preview,
        TransactionSource source,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Id, c.Name, c.Type })
            .ToListAsync(cancellationToken);

        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsArchived)
            .OrderBy(a => a.Name)
            .Select(a => new { a.Id, a.Name })
            .ToListAsync(cancellationToken);

        var context = new TransactionParsingContext(
            now,
            categories.Where(c => c.Type == CategoryType.Income).Select(c => c.Name).ToList(),
            categories.Where(c => c.Type == CategoryType.Expense).Select(c => c.Name).ToList(),
            accounts.Select(a => a.Name).ToList());

        var parsed = await _parser.ParseAsync(text, context, cancellationToken)
                     ?? throw new BusinessRuleException(
                         "No transaction could be found in the text. Mention an amount and what it was for.");

        // category: the parsed one, otherwise "Other" of the same type
        Guid? categoryId = null;
        string? categoryName = null;
        if (parsed.Type != TransactionType.Transfer)
        {
            var wanted = parsed.Type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense;
            var category = categories.FirstOrDefault(c => c.Type == wanted && SameName(c.Name, parsed.CategoryName))
                           ?? categories.FirstOrDefault(c => c.Type == wanted && SameName(c.Name, DefaultCategories.Other));
            categoryId = category?.Id;
            categoryName = category?.Name;
        }

        // account: the one the caller chose, otherwise the one named in the text, otherwise the only one
        var account = accountId is { } chosen
            ? accounts.FirstOrDefault(a => a.Id == chosen) ?? throw new NotFoundException("Account", chosen)
            : accounts.FirstOrDefault(a => SameName(a.Name, parsed.AccountName)) ?? (accounts.Count == 1 ? accounts[0] : null);

        var destination = parsed.Type == TransactionType.Transfer
            ? accounts.FirstOrDefault(a => SameName(a.Name, parsed.DestinationAccountName))
            : null;

        var date = parsed.Date ?? now;

        var draft = new ParsedTransactionDto(
            parsed.Type,
            parsed.Amount,
            date,
            parsed.Description,
            account?.Id,
            account?.Name,
            destination?.Id,
            destination?.Name,
            categoryId,
            categoryName,
            parsed.Method);

        if (preview)
            return new AiTransactionResultDto(transcript, draft, null);

        if (account is null)
            throw new BusinessRuleException("Could not tell which account to use. Choose one with 'accountId'.");

        if (parsed.Type == TransactionType.Transfer)
        {
            if (destination is null)
                throw new BusinessRuleException("Could not tell which account the money goes to.");
        }
        else if (categoryId is null)
        {
            throw new BusinessRuleException("No matching category was found. Create a category first.");
        }

        var transaction = await _sender.Send(
            new CreateTransactionCommand(
                parsed.Type,
                parsed.Amount,
                account.Id,
                categoryId,
                destination?.Id,
                date,
                parsed.Description,
                source),
            cancellationToken);

        return new AiTransactionResultDto(transcript, draft, transaction);
    }

    private static bool SameName(string name, string? other)
        => other is not null && string.Equals(name, other, StringComparison.OrdinalIgnoreCase);
}
