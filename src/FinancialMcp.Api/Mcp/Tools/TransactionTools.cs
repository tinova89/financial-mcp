using System.ComponentModel;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Application.Transactions.DeleteTransaction;
using FinancialMcp.Application.Transactions.GetTransaction;
using FinancialMcp.Application.Transactions.ConfirmTransaction;
using FinancialMcp.Application.Transactions.ListTransactions;
using FinancialMcp.Application.Transactions.UpdateTransaction;
using FinancialMcp.Domain.Enums;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

/// <summary>
/// MCP tools for transactions (checking account + credit card). Each tool is "thin": it
/// only builds the MediatR request and calls IMediator.Send — all business logic lives in
/// the handlers in FinancialMcp.Application (see CLAUDE.md > Mediator Pattern).
/// </summary>
[McpServerToolType]
public sealed class TransactionTools(IMediator mediator)
{
    [McpServerTool(Name = "list_transactions"), Description(
        """
        Lists transactions (checking-account or credit-card) with a broad set of optional
        filters, paginated.

        ## Parameters
        - **periodStart** / **periodEnd** — Inclusive `ExpectedDate` range. Required.
        - **type** — `int` enum (`TransactionType`), sent as a plain integer, not a string.
          Optional. Values: `1 - Expense`, `2 - Income`, `3 - Transfer`, `4 - Payment`.
        - **status** — `int` enum (`TransactionStatus`), sent as a plain integer, not a
          string. Optional. Values: `1 - Revision`, `2 - Scheduled`, `3 - Confirmed`.
        - **category** — Filter to rows whose category starts with this parent (e.g.
          `"Moradia"` matches both `Moradia` and `Moradia/Seguro`). Optional.
        - **subcategory** — Filter to rows with this exact subcategory. Optional; applied
          in memory alongside `parentCategory`.
        - **accountId** — `Guid` matching either a checking account (for its own
          transactions) or a credit card's own id (for that card's transactions) — both
          use the same `accountId` field on a transaction. Optional.
        - **year** / **month** — Filter by the transaction's *reference* month/year
          (`ConfirmedDate` for checking-account rows, `InvoiceDueDate` for credit-card
          rows — see `get_budget_status` for why these differ). Optional.
        - **page** — 1-based page number. Optional, defaults to `1`.
        - **pageSize** — Items per page. Optional, defaults to `50`.

        ## Behavior
        - Read-only; ordered by `expectedDate` descending.
        - `periodStart`/`periodEnd` are required; every other filter is optional. All
          provided filters combine with AND semantics.
        - Soft-deleted transactions are excluded automatically.

        ## Example
        ```json
        { "periodStart": "2026-08-01", "periodEnd": "2026-08-31", "status": 2, "year": 2026, "month": 8, "page": 1, "pageSize": 20 }
        ```
        (`status: 2` = Scheduled.)

        ## Returns
        A `PagedResult<TransactionDto>` (items, page, pageSize, totalCount, totalPages). Each
        item's `needsConfirmation` is `true` when it is `Scheduled` and its `expectedDate` is
        already in the past (it likely needs confirming; the status is never changed
        automatically). Every item's `remainingBudget`/`remainingBudgetPercentage` are always
        `null` here — only create/update/delete compute the category's remaining budget.
        """)]
    public Task<PagedResult<TransactionDto>> ListTransactionsAsync(
        DateOnly periodStart, DateOnly periodEnd,
        TransactionType? type = null, TransactionStatus? status = null,
        string? category = null, string? subcategory = null,
        Guid? accountId = null,
        int? year = null, int? month = null, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ListTransactionsQuery(
            periodStart, periodEnd, type, status, category, subcategory, accountId,
            year, month, page, pageSize), cancellationToken);

    [McpServerTool(Name = "get_transaction"), Description(
        """
        Fetches the full detail of a single transaction by id.

        ## Parameters
        - **transactionId** — `Guid` of the transaction to look up. Required.

        ## Behavior
        - Read-only.
        - Throws a not-found error if `transactionId` doesn't match a registered
          (non-deleted) transaction.

        ## Example
        ```json
        { "transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
        ```

        ## Returns
        The matching `TransactionDto` (id, type, status, description, amount, rawCategory,
        expectedDate, actualDate, confirmedDate, invoiceDueDate, recurrence,
        currentInstallment, totalInstallments, accountId, needsConfirmation).
        `needsConfirmation` is `true` for an overdue `Scheduled` transaction. `remainingBudget`/
        `remainingBudgetPercentage` are always `null` here — only create/update/delete
        compute the category's remaining budget.
        """)]
    public Task<TransactionDto> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetTransactionQuery(transactionId), cancellationToken);

    [McpServerTool(Name = "create_transaction"), Description(
        """
        Inserts a new transaction (checking-account or credit-card), honoring the required
        fields for each statement type. Fields are passed as a single object matching
        `CreateTransactionCommand`.

        ## Parameters (fields of the command object)
        - **type** — `int` enum (`TransactionType`), sent as a plain integer, not a string.
          Required. Values: `1 - Expense`, `2 - Income`, `3 - Transfer`, `4 - Payment`.
        - **status** — `int` enum (`TransactionStatus`), sent as a plain integer, not a
          string. Required. Values: `1 - Revision`, `2 - Scheduled`, `3 - Confirmed`.
        - **description** — Up to 256 characters. Required.
        - **amount** — Non-zero decimal. Required.
        - **rawCategory** — `"Categoria-mãe/Subcategoria"` (subcategory optional within
          the string, split on `/`). Required. Up to 256 characters.
        - **expectedDate** — The statement's "Data prevista". Required.
        - **actualDate** — The statement's "Data efetiva". Optional.
        - **confirmedDate** — Set when a checking-account row is confirmed. Required when
          `status = 3` (`Confirmed`); optional otherwise.
        - **invoiceDueDate** — Required when `accountId` refers to a credit card
          ("Venc. Fatura").
        - **recurrence** — `int` enum (`RecurrenceType`), sent as a plain integer, not a
          string. Required. Values: `0 - None`, `1 - Installment`, `2 - FixedMonthly`.
        - **currentInstallment** / **totalInstallments** — Required (and
          `totalInstallments >= currentInstallment`) when `recurrence = 1` (`Installment`).
        - **accountId** — `Guid` identifying the destination: a checking account for a
          plain checking-account transaction, or a credit card's own id for a credit-card
          transaction (a credit card is an Account row via EF Core TPH). Required.

        ## Not a parameter
        - There's no `source`/`type-of-account` flag — whether credit-card-specific rules
          apply (required `invoiceDueDate`, installment validation) is determined by
          looking up `accountId`'s actual `Account.Kind`, not a caller-supplied label that
          could disagree with what `accountId` really points at.

        ## Behavior
        - Non-destructive write; no confirmation is required.
        - Every row requires a non-empty `accountId` regardless of what kind of account it is.

        ## Example
        ```json
        { "type": 1, "status": 2,
          "description": "Notebook 6/12", "amount": -450.00, "rawCategory": "Eletrônicos",
          "expectedDate": "2026-08-05", "invoiceDueDate": "2026-08-12",
          "recurrence": 1, "currentInstallment": 6, "totalInstallments": 12,
          "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
        ```
        (`type: 1` = Expense, `status: 2` = Scheduled, `recurrence: 1` = Installment.)

        ## Returns
        The created `TransactionDto` (its `needsConfirmation` is `true` only for an overdue
        `Scheduled` row), including `remainingBudget`/`remainingBudgetPercentage`
        for the transaction's parent category (see CLAUDE.md > Business Rules > Budget goals):
        the goal amount minus Gasto_Real for the transaction's reference month, counting this
        new transaction. Both are `null` when no budget goal is in effect for that
        category/month (or the transaction has no resolvable reference month yet, e.g. an
        unconfirmed checking-account row with no `confirmedDate`).
        """)]
    public Task<TransactionDto> CreateTransactionAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default) =>
        mediator.Send(command, cancellationToken);

    [McpServerTool(Name = "update_transaction"), Description(
        """
        Changes one or more fields of an existing transaction. This is a partial patch:
        only the fields explicitly provided on the command object are changed — anything
        left `null` keeps its current stored value.

        ## Parameters (fields of the command object)
        - **transactionId** — `Guid` of the transaction to update. Required.
        - **status** — `int` enum (`TransactionStatus`), sent as a plain integer, not a
          string. Optional. Values: `1 - Revision`, `2 - Scheduled`, `3 - Confirmed`.
          Moving into `Scheduled`/`Confirmed` stamps `scheduledAt`/`confirmedAt` once.
        - **rawCategory** — New `"Categoria-mãe/Subcategoria"`. Optional. Up to 256 characters.
        - **amount** — New amount. Optional.
        - **expectedDate** / **actualDate** / **confirmedDate** / **invoiceDueDate** — New
          dates. Optional.

        ## Not patchable here
        - **accountId**, **recurrence**, and installment fields cannot be changed through
          this tool — recreate the transaction if those need to differ.

        ## Behavior
        - Throws a not-found error if `transactionId` doesn't match a registered transaction.
        - Non-destructive; no confirmation is required.

        ## Example
        ```json
        { "transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "status": 3, "confirmedDate": "2026-08-10" }
        ```
        (`status: 3` = Confirmed.)

        ## Returns
        The updated `TransactionDto` (with `needsConfirmation`), including
        `remainingBudget`/`remainingBudgetPercentage` for the transaction's (possibly newly
        changed) parent category and reference month — same rules as `create_transaction`'s
        `Returns` section.
        """)]
    public Task<TransactionDto> UpdateTransactionAsync(UpdateTransactionCommand command, CancellationToken cancellationToken = default) =>
        mediator.Send(command, cancellationToken);

    [McpServerTool(Name = "delete_transaction"), Description(
        """
        Removes a transaction via soft delete (sets `IsDeleted`/`DeletedAt`; the row is
        never physically removed and disappears from all default queries afterwards).

        ## Parameters
        - **transactionId** — `Guid` of the transaction to remove. Required.
        - **confirm** — Must be explicitly `true` for the operation to proceed. Required.

        ## DESTRUCTIVE OPERATION
        Always confirm explicitly with the user before calling this tool with
        `confirm = true`. Calling it with `confirm = false` (or omitted) is rejected by
        validation before any data is touched.

        ## Behavior
        - Throws a not-found error if `transactionId` doesn't match a registered transaction.

        ## Example
        ```json
        { "transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "confirm": true }
        ```

        ## Returns
        The deleted transaction's `TransactionDto` (its stored fields, unchanged), with
        `remainingBudget`/`remainingBudgetPercentage` recomputed for its parent category and
        reference month as if this transaction no longer counts toward Gasto_Real — same
        null-ability rules as `create_transaction`'s `Returns` section.
        """)]
    public Task<TransactionDto> DeleteTransactionAsync(Guid transactionId, bool confirm, CancellationToken cancellationToken = default) =>
        mediator.Send(new DeleteTransactionCommand(transactionId, confirm), cancellationToken);

    [McpServerTool(Name = "confirm_transaction"), Description(
        """
        Moves a `Scheduled` transaction to `Confirmed` (it actually happened).

        ## Parameters
        - **transactionId** — `Guid` of the transaction to confirm. Required.
        - **confirmedDate** — Date to record as the confirmation date. Optional; defaults
          to today (UTC) when omitted.

        ## Precondition
        - The transaction's current `status` **must** be `Scheduled` (`2`). Confirming a
          `Revision` (`1`) or an already-`Confirmed` (`3`) transaction is rejected by
          validation before any data is touched.

        ## Behavior
        - Throws a not-found error if `transactionId` doesn't match a registered transaction.
        - Sets `status = Confirmed` and stamps `confirmedAt` once (an existing `confirmedAt`
          is never overwritten).
        - Only sets `confirmedDate` when the transaction's account is a plain checking
          account (`Account.Kind != Credit`) — for a credit-card-sourced transaction,
          `confirmedDate` is left untouched since the relevant reference date for that row
          is `invoiceDueDate`, not `confirmedDate`.
        - Publishes a `TransactionConfirmedNotification` afterwards, which downstream
          handlers can use to recalculate cached budget status or notify other clients —
          this doesn't block or change the response of this call.
        - Non-destructive; no confirmation flag is required.

        ## Example
        ```json
        { "transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "confirmedDate": "2026-08-10" }
        ```

        ## Returns
        A confirmation message string ("Transação confirmada.").
        """)]
    public async Task<string> ConfirmTransactionAsync(
        Guid transactionId, DateOnly? confirmedDate = null, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new ConfirmTransactionCommand(transactionId, confirmedDate), cancellationToken);
        return "Transação confirmada.";
    }
}
