using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.CreditCards.DeleteCreditCard;

/// <summary>
/// Removes (soft delete) a credit card. Destructive operation: Confirm must be
/// explicitly true, validated by ValidationBehavior (see CLAUDE.md > Mediator
/// Pattern > Destructive operations / What Claude Should Avoid).
/// </summary>
public sealed record DeleteCreditCardCommand(Guid CreditCardId, bool Confirm)
    : IRequest, ITransactionalRequest;
