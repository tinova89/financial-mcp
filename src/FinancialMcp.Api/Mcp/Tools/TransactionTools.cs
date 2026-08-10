using System.ComponentModel;
using System.Transactions;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Application.Transactions.DeleteTransaction;
using FinancialMcp.Application.Transactions.GetTransaction;
using FinancialMcp.Application.Transactions.ListTransactions;
using FinancialMcp.Application.Transactions.ReconcileTransaction;
using FinancialMcp.Application.Transactions.UpdateTransaction;
using FinancialMcp.Domain.Enums;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

/// <summary>
/// MCP tools for transactions (checking account + credit card). Each tool is "thin": it
/// only builds the MediatR request and calls IMediator.Send — all business logic lives in
/// the handlers in FinancialMcp.Application (see CLAUDE.md > Mediator Pattern).
/// </summary>
[McpServerToolType]
public sealed class TransactionTools(IMediator mediator)
{
    [McpServerTool(Name = "list_transactions"), Description(
        "Lists checking account and/or credit card transactions with filters " +
        "(type, status, category/subcategory, account, card, period, reference month).")]
    public Task<PagedResult<TransactionDto>> ListTransactionsAsync(
        TransactionSource? source = null, TransactionType? type = null, Domain.Enums.TransactionStatus? status = null,
        string? parentCategory = null, string? subcategory = null,
        Guid? accountId = null, Guid? cardId = null,
        DateOnly? periodStart = null, DateOnly? periodEnd = null,
        int? year = null, int? month = null, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ListTransactionsQuery(
            source, type, status, parentCategory, subcategory, accountId, cardId,
            periodStart, periodEnd, year, month, page, pageSize), cancellationToken);

    [McpServerTool(Name = "get_transaction"), Description("Detail of a specific transaction.")]
    public Task<TransactionDto> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetTransactionQuery(transactionId), cancellationToken);

    [McpServerTool(Name = "create_transaction"), Description(
        "Inserts a new transaction (checking account or credit card), honoring " +
        "the required fields for each statement type.")]
    public Task<TransactionDto> CreateTransactionAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default) =>
        mediator.Send(command, cancellationToken);

    [McpServerTool(Name = "update_transaction"), Description(
        "Changes fields of an existing transaction (status, category, amount, date).")]
    public Task<TransactionDto> UpdateTransactionAsync(UpdateTransactionCommand command, CancellationToken cancellationToken = default) =>
        mediator.Send(command, cancellationToken);

    [McpServerTool(Name = "delete_transaction"), Description(
        "Removes (soft delete) a transaction. DESTRUCTIVE OPERATION: requires confirm = true. " +
        "Always confirm explicitly with the user before calling this tool with confirm = true.")]
    public async Task<string> DeleteTransactionAsync(Guid transactionId, bool confirm, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DeleteTransactionCommand(transactionId, confirm), cancellationToken);
        return "Transação removida (soft delete).";
    }

    [McpServerTool(Name = "reconcile_transaction"), Description(
        "Marks a transaction as Reconciled (checking account) or the equivalent for credit card.")]
    public async Task<string> ReconcileTransactionAsync(
        Guid transactionId, DateOnly? reconciledDate = null, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new ReconcileTransactionCommand(transactionId, reconciledDate), cancellationToken);
        return "Transação conciliada.";
    }
}
