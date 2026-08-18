using System.ComponentModel;
using FinancialMcp.Application.CreditCards.CreateCreditCard;
using FinancialMcp.Application.CreditCards.GetCreditCard;
using FinancialMcp.Application.CreditCards.ListCreditCards;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

/// <summary>
/// MCP tools for credit cards (read-only). A credit card is a kind of Account (Kind is
/// always "Credit"), sharing the same "accounts" table via EF Core TPH, plus its own
/// ClosingDay/DueDay/PaymentAccountId fields. Create/update/delete are exposed via the
/// REST API instead (see Program.cs > Credit Cards API). Each tool here is "thin": it
/// only builds the MediatR request and calls IMediator.Send — all business logic lives
/// in the handlers in FinancialMcp.Application (see CLAUDE.md > Mediator Pattern).
/// </summary>
[McpServerToolType]
public sealed class CreditCardTools(IMediator mediator)
{
    [McpServerTool(Name = "list_credit_cards"), Description(
        """
        Lists every registered credit card.

        ## Parameters
        None — this tool takes no filters and always returns the full set of credit cards.

        ## Behavior
        - Read-only; ordered by `displayName`.
        - Soft-deleted credit cards are excluded automatically (shared with Account's
          global query filter via EF Core TPH).
        - Plain (non-credit-card) accounts never appear here — use `list_accounts` for those.

        ## Returns
        A list of `CreditCardDto` (id, displayName, bankCode, initialAmount, kind,
        baseCurrencyCode, closingDay, dueDay, paymentAccountId).
        """)]
    public Task<IReadOnlyList<CreditCardDto>> ListCreditCardsAsync(CancellationToken cancellationToken = default) =>
        mediator.Send(new ListCreditCardsQuery(), cancellationToken);

    [McpServerTool(Name = "get_credit_card"), Description(
        """
        Fetches the full detail of a single credit card by id.

        ## Parameters
        - **creditCardId** — `Guid` of the credit card to look up. Required.

        ## Behavior
        - Read-only.
        - Throws a not-found error if `creditCardId` doesn't match a registered
          (non-deleted) credit card.

        ## Example
        ```json
        { "creditCardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
        ```

        ## Returns
        The matching `CreditCardDto` (id, displayName, bankCode, initialAmount, kind,
        baseCurrencyCode, closingDay, dueDay, paymentAccountId).
        """)]
    public Task<CreditCardDto> GetCreditCardAsync(Guid creditCardId, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetCreditCardQuery(creditCardId), cancellationToken);
}
