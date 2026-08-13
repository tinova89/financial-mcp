using FinancialApp.Model;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Common.Services;

/// <summary>See ITransactionCategoryResolver.</summary>
public sealed class TransactionCategoryResolver(IApplicationDbContext db) : ITransactionCategoryResolver
{
    public async Task ResolveAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var (parentName, subcategoryName) = transaction.SplitRawCategory();

        var parent = await GetOrCreateAsync(parentName, parentCategoryId: null, transaction.Type, cancellationToken);

        var category = subcategoryName is null
            ? parent
            : await GetOrCreateAsync(subcategoryName, parent.Id, transaction.Type, cancellationToken);

        transaction.Category = category;
        transaction.CategoryId = category.Id;
    }

    private async Task<TransactionCategory> GetOrCreateAsync(
        string name, Guid? parentCategoryId, TransactionType transactionType, CancellationToken cancellationToken)
    {
        // Check not-yet-saved categories from earlier in the same batch (e.g. import_statement
        // processing many rows before the single SaveChangesAsync) before hitting the database.
        var existing = db.TransactionCategories.Local.FirstOrDefault(c =>
            c.ParentCategoryId == parentCategoryId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        existing ??= await db.TransactionCategories.FirstOrDefaultAsync(c =>
            c.ParentCategoryId == parentCategoryId && c.Name.ToLower() == name.ToLower(), cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = new TransactionCategory
        {
            Name = name,
            ParentCategoryId = parentCategoryId,
            Operation = MapOperation(transactionType)
        };

        db.TransactionCategories.Add(created);

        return created;
    }

    private static FinancialOperationKind MapOperation(TransactionType type) => type switch
    {
        TransactionType.Expense => FinancialOperationKind.Expense,
        TransactionType.Income => FinancialOperationKind.Income,
        _ => FinancialOperationKind.Transfer // Transfer and Payment (card bill payment) both move money between accounts.
    };
}
