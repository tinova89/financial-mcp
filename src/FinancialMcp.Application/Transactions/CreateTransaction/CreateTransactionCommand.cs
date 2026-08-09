using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Inserts a new transaction (checking account or credit card), honoring the required
/// fields for each statement type. Corresponds to the MCP tool `create_transaction` (see CLAUDE.md > MCP).
/// </summary>
public sealed record CreateTransactionCommand(
    string Source,             // "ContaCorrente" | "CartaoCredito"
    string Type,                // "Despesa" | "Receita" | "Transferencia" | "Pagamento"
    string Status,
    string Description,
    decimal Amount,
    string RawCategory,        // "Categoria-mãe/Subcategoria"
    DateOnly ExpectedDate,
    DateOnly? ActualDate,
    DateOnly? ReconciledDate,
    DateOnly? InvoiceDueDate,
    string? Recurrence,
    int? CurrentInstallment,
    int? TotalInstallments,
    Guid? AccountId,
    Guid? CardId) : IRequest<TransactionDto>, ITransactionalRequest;
