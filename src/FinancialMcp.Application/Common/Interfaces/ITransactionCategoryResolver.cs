using FinancialMcp.Domain.Entities;

namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>
/// Get-or-creates the persisted TransactionCategory (parent + optional subcategory) for a
/// transaction's RawCategory and assigns it to Transaction.Category/CategoryId. This is the
/// only place TransactionCategory rows get created — there is no dedicated category CRUD tool.
/// </summary>
public interface ITransactionCategoryResolver
{
    Task ResolveAsync(Transaction transaction, CancellationToken cancellationToken);
}
