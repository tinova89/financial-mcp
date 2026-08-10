using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Accounts.DeleteAccount;

/// <summary>
/// Removes (soft delete) an account. Destructive operation: Confirm must be
/// explicitly true, validated by ValidationBehavior (see CLAUDE.md > Mediator
/// Pattern > Destructive operations / What Claude Should Avoid).
/// </summary>
public sealed record DeleteAccountCommand(Guid AccountId, bool Confirm)
    : IRequest, ITransactionalRequest;
