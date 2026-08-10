using FinancialMcp.Application.CreditCards.CreateCreditCard;
using MediatR;

namespace FinancialMcp.Application.CreditCards.ListCreditCards;

/// <summary>Lists all registered credit cards. Corresponds to the MCP tool `list_credit_cards`.</summary>
public sealed record ListCreditCardsQuery : IRequest<IReadOnlyList<CreditCardDto>>;
