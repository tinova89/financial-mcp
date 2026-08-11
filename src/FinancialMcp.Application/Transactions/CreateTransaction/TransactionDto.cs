namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>Response DTO — never expose the Transaction domain entity directly (see CLAUDE.md > DTOs).</summary>
public sealed record TransactionDto(
    Guid Id,
    string Source,
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
    Guid AccountId);
