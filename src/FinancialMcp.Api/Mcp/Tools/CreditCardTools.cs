using System.ComponentModel;
using FinancialMcp.Application.CreditCards.CreateCreditCard;
using FinancialMcp.Application.CreditCards.DeleteCreditCard;
using FinancialMcp.Application.CreditCards.GetCreditCard;
using FinancialMcp.Application.CreditCards.ListCreditCards;
using FinancialMcp.Application.CreditCards.UpdateCreditCard;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

/// <summary>
/// MCP tools for credit cards. A credit card is a kind of Account (Kind is always
/// "Credit"), sharing the same "accounts" table via EF Core TPH, plus its own
/// ClosingDay/DueDay/PaymentAccountId fields. Each tool is "thin": it only builds the
/// MediatR request and calls IMediator.Send — all business logic lives in the handlers
/// in FinancialMcp.Application (see CLAUDE.md > Mediator Pattern).
/// </summary>
[McpServerToolType]
public sealed class CreditCardTools(IMediator mediator)
{
    [McpServerTool(Name = "create_credit_card"), Description(
        """
        Registers a new credit card, linked to the Account debited when its bill is paid.

        ## Parameters
        - **bankCode** — COMPE bank code identifying the institution. Supported values:
          `341` (Itaú), `260` (Nubank), `077` (Inter), `307` (Wallet). An unrecognized
          code is rejected by validation before persisting.
        - **displayName** — Friendly name shown to the user (e.g. `"Nubank Mastercard"`).
          Required, up to 200 characters.
        - **initialAmount** — Opening balance for the card, expressed in `baseCurrencyCode`.
        - **baseCurrencyCode** — ISO currency code. Supported values: `BRL`, `USD`, `BTC`.
          An unrecognized code is rejected by validation before persisting.
        - **closingDay** — Day of the month the bill closes. Must be between 1 and 28
          (capped at 28 so the value is valid in every month, avoiding month-end edge cases).
        - **dueDay** — Day of the month the bill is due. Same 1-28 range.
        - **paymentAccountId** — `Guid` of the *other* account debited to pay this card's
          bill. Must reference an existing account that is **not itself a credit card**
          (rejected with a not-found error otherwise) — this prevents a card being
          configured to pay itself via another card.

        ## Not a parameter
        - **kind** is intentionally absent — every credit card's `Kind` is always forced
          to `"Credit"` by the handler, never settable here.

        ## Behavior
        - Non-destructive write; no confirmation is required.

        ## Example
        ```json
        { "bankCode": "260", "displayName": "Nubank Mastercard", "initialAmount": 0,
          "baseCurrencyCode": "BRL", "closingDay": 5, "dueDay": 12,
          "paymentAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
        ```

        ## Returns
        The created `CreditCardDto` (id, displayName, bankCode, initialAmount, kind,
        baseCurrencyCode, closingDay, dueDay, paymentAccountId).
        """)]
    public Task<CreditCardDto> CreateCreditCardAsync(
        string bankCode, string displayName, decimal initialAmount, string baseCurrencyCode,
        byte closingDay, byte dueDay, Guid paymentAccountId, CancellationToken cancellationToken = default) =>
        mediator.Send(new CreateCreditCardCommand(
            bankCode, displayName, baseCurrencyCode, initialAmount, closingDay, dueDay, paymentAccountId), cancellationToken);

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

    [McpServerTool(Name = "update_credit_card"), Description(
        """
        Changes one or more fields of an existing credit card. This is a partial patch:
        only the parameters explicitly provided are changed — any parameter left as
        `null` keeps its current stored value.

        ## Parameters
        - **creditCardId** — `Guid` of the credit card to update. Required.
        - **displayName** — New friendly name, up to 200 characters. Optional.
        - **bankCode** — New COMPE bank code (see `create_credit_card` for supported
          values). Optional; rejected by validation if unrecognized.
        - **initialAmount** — New opening balance. Optional.
        - **baseCurrencyCode** — New ISO currency code (`BRL`, `USD`, `BTC`). Optional;
          rejected by validation if unrecognized.
        - **closingDay** — New closing day (1-28). Optional.
        - **dueDay** — New due day (1-28). Optional.
        - **paymentAccountId** — New payment account. Optional; must reference an
          existing, non-credit-card account (rejected with a not-found error otherwise).

        ## Not a parameter
        - **kind** cannot be changed — it's always `"Credit"`.

        ## Behavior
        - Throws a not-found error if `creditCardId` doesn't match a registered credit card.
        - Non-destructive; no confirmation is required.

        ## Example
        ```json
        { "creditCardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "closingDay": 10, "dueDay": 17 }
        ```

        ## Returns
        The updated `CreditCardDto` (id, displayName, bankCode, initialAmount, kind,
        baseCurrencyCode, closingDay, dueDay, paymentAccountId).
        """)]
    public Task<CreditCardDto> UpdateCreditCardAsync(
        Guid creditCardId, string? displayName = null, string? bankCode = null, decimal? initialAmount = null,
        string? baseCurrencyCode = null, byte? closingDay = null, byte? dueDay = null,
        Guid? paymentAccountId = null, CancellationToken cancellationToken = default) =>
        mediator.Send(new UpdateCreditCardCommand(
            creditCardId, displayName, bankCode, initialAmount, baseCurrencyCode,
            closingDay, dueDay, paymentAccountId), cancellationToken);

    [McpServerTool(Name = "delete_credit_card"), Description(
        """
        Removes a credit card via soft delete (sets `IsDeleted`/`DeletedAt`; the row is
        never physically removed and disappears from all default queries afterwards).

        ## Parameters
        - **creditCardId** — `Guid` of the credit card to remove. Required.
        - **confirm** — Must be explicitly `true` for the operation to proceed. Required.

        ## DESTRUCTIVE OPERATION
        Always confirm explicitly with the user before calling this tool with
        `confirm = true`. Calling it with `confirm = false` (or omitted) is rejected by
        validation before any data is touched.

        ## Behavior
        - Throws a not-found error if `creditCardId` doesn't match a registered credit card.
        - Does not cascade to linked transactions — they keep their `cardId` reference;
          consider that before deleting a credit card still in active use.

        ## Example
        ```json
        { "creditCardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "confirm": true }
        ```

        ## Returns
        A confirmation message string ("Cartão de crédito removido (soft delete).").
        """)]
    public async Task<string> DeleteCreditCardAsync(Guid creditCardId, bool confirm, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DeleteCreditCardCommand(creditCardId, confirm), cancellationToken);
        return "Cartão de crédito removido (soft delete).";
    }
}
