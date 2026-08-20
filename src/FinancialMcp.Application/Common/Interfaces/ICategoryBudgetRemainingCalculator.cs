using FinancialMcp.Domain.Entities;

namespace FinancialMcp.Application.Common.Interfaces;

/// <summary>
/// Computes the remaining budget (Saldo_Meta) and remaining percentage for a single
/// transaction's parent category/reference month, applying the same rules as
/// GetBudgetStatusQueryHandler (see CLAUDE.md > Business Rules > Budget goals). Used by
/// create_transaction/update_transaction/delete_transaction to report the post-write budget
/// status for the affected category alongside the transaction itself.
/// </summary>
public interface ICategoryBudgetRemainingCalculator
{
    /// <param name="transaction">
    /// The transaction being written — its Category (with ParentCategory loaded), AccountId,
    /// Status/Type/Amount and reference dates must already reflect the state to calculate
    /// against (e.g. the updated fields, before SaveChangesAsync has run).
    /// </param>
    /// <param name="includeTransaction">
    /// True to count this transaction's own amount toward Gasto_Real (create/update, where the
    /// transaction still exists after the write); false to exclude it entirely (delete).
    /// </param>
    Task<CategoryBudgetRemaining> CalculateAsync(Transaction transaction, bool includeTransaction, CancellationToken cancellationToken);
}

/// <summary>Null fields mean no budget goal is currently in effect for the category/reference month.</summary>
public sealed record CategoryBudgetRemaining(decimal? RemainingBudget, decimal? RemainingBudgetPercentage)
{
    public static readonly CategoryBudgetRemaining None = new(null, null);
}
