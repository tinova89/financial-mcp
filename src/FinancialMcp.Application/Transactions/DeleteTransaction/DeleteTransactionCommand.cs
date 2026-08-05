using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Transactions.DeleteTransaction;

/// <summary>
/// Remove (soft delete) uma transação. Operação destrutiva: Confirm deve ser
/// explicitamente true, validado pelo ValidationBehavior (ver CLAUDE.md > Padrão
/// Mediator > Operações destrutivas / O que o Claude Deve Evitar).
/// </summary>
public sealed record DeleteTransactionCommand(Guid TransactionId, bool Confirm)
    : IRequest, ITransactionalRequest;
