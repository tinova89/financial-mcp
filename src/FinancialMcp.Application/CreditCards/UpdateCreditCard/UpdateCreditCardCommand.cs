using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Application.CreditCards.CreateCreditCard;
using MediatR;

namespace FinancialMcp.Application.CreditCards.UpdateCreditCard;

/// <summary>
/// Changes fields of an existing credit card. Corresponds to the MCP tool `update_credit_card`.
/// Null fields are ignored (partial patch). Kind is never patched — always Credit.
/// </summary>
public sealed record UpdateCreditCardCommand(
    Guid CreditCardId,
    string? DisplayName = null,
    string? BankCode = null,
    decimal? InitialAmount = null,
    string? BaseCurrencyCode = null,
    byte? ClosingDay = null,
    byte? DueDay = null,
    Guid? PaymentAccountId = null) : IRequest<CreditCardDto>, ITransactionalRequest;
