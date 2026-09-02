namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>Response DTO — never expose the Transaction domain entity directly (see CLAUDE.md > DTOs).</summary>
/// <param name="RemainingBudget">
/// Saldo_Meta for the transaction's parent category/reference month (goal amount minus
/// Gasto_Real, computed as of this write — see ICategoryBudgetRemainingCalculator). Null when
/// no budget goal is in effect for that category/month, or when the tool doesn't compute it
/// (get_transaction/list_transactions never populate this field).
/// </param>
/// <param name="RemainingBudgetPercentage">
/// RemainingBudget as a fraction of the goal amount (e.g. 0.42 means 42% of the budget is
/// still unspent) — null under the same conditions as RemainingBudget, or when the goal
/// amount is zero.
/// </param>
/// <param name="NeedsConfirmation">
/// True when <c>Status = Scheduled</c> and <c>ExpectedDate</c> is before today — the
/// transaction was expected to have happened by now and likely needs confirming. Purely
/// informational; the status is never changed automatically (Card #14).
/// </param>
public sealed record TransactionDto(
    Guid Id,
    string Type,
    string Status,
    string Description,
    decimal Amount,
    string RawCategory,
    DateOnly ExpectedDate,
    DateOnly? ActualDate,
    DateOnly? ReconciledDate,
    DateOnly? InvoiceDueDate,
    string? Recurrence,
    int? CurrentInstallment,
    int? TotalInstallments,
    Guid AccountId,
    bool NeedsConfirmation,
    decimal? RemainingBudget = null,
    decimal? RemainingBudgetPercentage = null);
