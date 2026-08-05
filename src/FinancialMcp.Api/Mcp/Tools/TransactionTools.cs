using System.ComponentModel;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Application.Transactions.DeleteTransaction;
using FinancialMcp.Application.Transactions.GetTransaction;
using FinancialMcp.Application.Transactions.ListTransactions;
using FinancialMcp.Application.Transactions.ReconcileTransaction;
using FinancialMcp.Application.Transactions.UpdateTransaction;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

/// <summary>
/// Ferramentas MCP de transações (CC + CD). Cada tool é "thin": apenas monta o
/// request do MediatR e chama IMediator.Send — toda a regra de negócio vive nos
/// handlers em FinancialMcp.Application (ver CLAUDE.md > Padrão Mediator).
/// </summary>
[McpServerToolType]
public sealed class TransactionTools(IMediator mediator)
{
    [McpServerTool(Name = "list_transactions"), Description(
        "Lista transações de Conta Corrente e/ou Cartão de Crédito com filtros " +
        "(tipo, status, categoria/subcategoria, conta, cartão, período, mês de referência).")]
    public Task<PagedResult<TransactionDto>> ListTransactionsAsync(
        string? origem = null, string? tipo = null, string? status = null,
        string? categoriaMae = null, string? subcategoria = null,
        Guid? contaId = null, Guid? cartaoId = null,
        DateOnly? periodoInicio = null, DateOnly? periodoFim = null,
        int? ano = null, int? mes = null, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ListTransactionsQuery(
            origem, tipo, status, categoriaMae, subcategoria, contaId, cartaoId,
            periodoInicio, periodoFim, ano, mes, page, pageSize), cancellationToken);

    [McpServerTool(Name = "get_transaction"), Description("Detalhe de uma transação específica.")]
    public Task<TransactionDto> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetTransactionQuery(transactionId), cancellationToken);

    [McpServerTool(Name = "create_transaction"), Description(
        "Insere uma nova transação (Conta Corrente ou Cartão de Crédito), respeitando " +
        "os campos obrigatórios de cada extrato.")]
    public Task<TransactionDto> CreateTransactionAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default) =>
        mediator.Send(command, cancellationToken);

    [McpServerTool(Name = "update_transaction"), Description(
        "Altera campos de uma transação existente (status, categoria, valor, data).")]
    public Task<TransactionDto> UpdateTransactionAsync(UpdateTransactionCommand command, CancellationToken cancellationToken = default) =>
        mediator.Send(command, cancellationToken);

    [McpServerTool(Name = "delete_transaction"), Description(
        "Remove (soft delete) uma transação. OPERAÇÃO DESTRUTIVA: exige confirm = true. " +
        "Sempre confirme explicitamente com o usuário antes de chamar esta ferramenta com confirm = true.")]
    public async Task<string> DeleteTransactionAsync(Guid transactionId, bool confirm, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DeleteTransactionCommand(transactionId, confirm), cancellationToken);
        return "Transação removida (soft delete).";
    }

    [McpServerTool(Name = "reconcile_transaction"), Description(
        "Marca uma transação como Conciliado (Conta Corrente) ou equivalente em Cartão de Crédito.")]
    public async Task<string> ReconcileTransactionAsync(
        Guid transactionId, DateOnly? dataConciliado = null, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new ReconcileTransactionCommand(transactionId, dataConciliado), cancellationToken);
        return "Transação conciliada.";
    }
}
