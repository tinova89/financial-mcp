using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.CreditCards.CreateCreditCard;

/// <summary>
/// Registers a new credit card, linked to the Account debited when its bill is paid
/// (PaymentAccountId). Kind is always forced to FinancialAccountKind.Credit by the
/// handler — not a parameter here. Corresponds to the MCP tool `create_credit_card`.
/// </summary>
public sealed record CreateCreditCardCommand(
    string BankCode,
    string DisplayName,
    string BaseCurrencyCode,
    decimal InitialAmount,
    byte ClosingDay,
    byte DueDay,
    Guid PaymentAccountId
    ) : IRequest<CreditCardDto>, ITransactionalRequest;
