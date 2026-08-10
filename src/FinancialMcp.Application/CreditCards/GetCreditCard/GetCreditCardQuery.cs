using FinancialMcp.Application.CreditCards.CreateCreditCard;
using MediatR;

namespace FinancialMcp.Application.CreditCards.GetCreditCard;

/// <summary>Detail of a specific credit card. Corresponds to the MCP tool `get_credit_card`.</summary>
public sealed record GetCreditCardQuery(Guid CreditCardId) : IRequest<CreditCardDto>;
