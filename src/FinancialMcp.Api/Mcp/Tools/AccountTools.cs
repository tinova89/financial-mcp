using FinancialApp.Model;
using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Accounts.DeleteAccount;
using FinancialMcp.Application.Accounts.GetAccount;
using FinancialMcp.Application.Accounts.ListAccounts;
using FinancialMcp.Application.Accounts.UpdateAccount;
using MediatR;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace FinancialMcp.Api.Mcp.Tools;

/// <summary>
/// MCP tools for financial accounts (bank accounts that Transactions and CreditCards link to).
/// Credit cards are a distinct MCP-managed type (see CreditCardTools) even though they share
/// the "accounts" table via EF Core TPH — these tools only ever operate on non-CreditCard accounts.
/// Each tool is "thin": it only builds the MediatR request and calls IMediator.Send —
/// all business logic lives in the handlers in FinancialMcp.Application (see CLAUDE.md > Mediator Pattern).
/// </summary>
[McpServerToolType]
public sealed class AccountTools(IMediator mediator)
{
    [McpServerTool(Name = "create_account"), Description(
        """
        Registers a new financial account (checking, credit, investment, or wallet) so that
        transactions and cards can be linked to it.

        ## Parameters
        - **bankCode** — COMPE bank code identifying the institution. Supported values:
          `341` (Itaú), `260` (Nubank), `077` (Inter), `307` (Wallet). An unrecognized
          code is rejected by validation before persisting.
        - **displayName** — Friendly name shown to the user (e.g. `"Nubank Corrente"`).
          Required, up to 200 characters.
        - **initialAmount** — Opening balance for the account, expressed in `baseCurrency`.
        - **kind** — Account classification, one of: `Credit`, `Debit`, `Investment`, `Wallet`.
          Drives reporting and business rules (e.g. credit vs. debit vs. investment behavior).
        - **baseCurrency** — ISO currency code for the account's balance. Supported values:
          `BRL`, `USD`, `BTC`. An unrecognized code is rejected by validation before persisting.

        ## Behavior
        - The account is created with no linked credit cards (`creditCardIds` starts
          empty); link credit cards afterwards via `create_credit_card`'s
          `paymentAccountId` parameter (see `CreditCardTools`).
        - This is a non-destructive write; no confirmation is required.

        ## Example
        ```json
        { "bankCode": "260", "displayName": "Nubank Corrente", "initialAmount": 1500.00, "kind": "Debit", "baseCurrency": "BRL" }
        ```

        ## Returns
        The created `AccountDto` (id, displayName, bankCode, creditCardIds).
        """)]
    public Task<AccountDto> CreateAccountAsync(string bankCode, string displayName, decimal initialAmount, string kind, string baseCurrency, CancellationToken cancellationToken = default) =>
        mediator.Send(new CreateAccountCommand(bankCode, displayName, baseCurrency, initialAmount, Enum.Parse<FinancialAccountKind>(kind)), cancellationToken);
    
    [McpServerTool(Name = "list_accounts"), Description(
        """
        Lists every registered non-credit-card financial account (checking, investment,
        wallet, etc. — use `list_credit_cards` for credit cards).

        ## Parameters
        None — this tool takes no filters and always returns the full set of accounts.

        ## Behavior
        - Read-only; ordered by `displayName`.
        - Each item includes the `creditCardIds` of credit cards whose bill is paid from
          that account, so a card's payment account can be cross-referenced without a
          separate call.
        - Soft-deleted accounts are excluded automatically (global query filter).

        ## Returns
        A list of `AccountDto` (id, displayName, bankCode, initialAmount, kind, baseCurrencyCode, creditCardIds).
        """)]
    public Task<IReadOnlyList<AccountDto>> ListAccountsAsync(CancellationToken cancellationToken = default) =>
        mediator.Send(new ListAccountsQuery(), cancellationToken);

    [McpServerTool(Name = "get_account"), Description(
        """
        Fetches the full detail of a single financial account by id.

        ## Parameters
        - **accountId** — `Guid` of the account to look up. Required.

        ## Behavior
        - Read-only.
        - Throws a not-found error if `accountId` doesn't match a registered, non-credit-card
          account (use `get_credit_card` for credit cards — they share the same id space
          but are managed exclusively through `CreditCardTools`).
        - Includes the `creditCardIds` of credit cards whose bill is paid from this account.

        ## Example
        ```json
        { "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
        ```

        ## Returns
        The matching `AccountDto` (id, displayName, bankCode, initialAmount, kind, baseCurrencyCode, creditCardIds).
        """)]
    public Task<AccountDto> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetAccountQuery(accountId), cancellationToken);

    [McpServerTool(Name = "update_account"), Description(
        """
        Changes one or more fields of an existing account. This is a partial patch: only the
        parameters explicitly provided are changed — any parameter left as `null` keeps its
        current stored value.

        ## Parameters
        - **accountId** — `Guid` of the account to update. Required.
        - **displayName** — New friendly name, up to 200 characters. Optional.
        - **bankCode** — New COMPE bank code. Must be one of the supported codes: `341` (Itaú),
          `260` (Nubank), `077` (Inter), `307` (Wallet). Optional; rejected by validation if
          unrecognized.
        - **initialAmount** — New opening balance, expressed in the account's `baseCurrencyCode`.
          Optional.
        - **kind** — New account classification: `Credit`, `Debit`, `Investment`, or `Wallet`.
          Optional; rejected by validation if not one of these values.
        - **baseCurrencyCode** — New ISO currency code. Supported values: `BRL`, `USD`, `BTC`.
          Optional; rejected by validation if unrecognized.

        ## Behavior
        - Throws a not-found error if `accountId` doesn't match a registered, non-credit-card account.
        - Non-destructive; no confirmation is required.
        - Existing linked credit cards (`creditCardIds`) are unaffected by this call.

        ## Example
        ```json
        { "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "displayName": "Nubank Corrente 2", "initialAmount": 2000.00 }
        ```

        ## Returns
        The updated `AccountDto` (id, displayName, bankCode, initialAmount, kind, baseCurrencyCode, creditCardIds).
        """)]
    public Task<AccountDto> UpdateAccountAsync(
        Guid accountId, string? displayName = null, string? bankCode = null, decimal? initialAmount = null,
        string? kind = null, string? baseCurrencyCode = null, CancellationToken cancellationToken = default) =>
        mediator.Send(new UpdateAccountCommand(
            accountId, displayName, bankCode, initialAmount,
            kind is null ? null : Enum.Parse<FinancialAccountKind>(kind), baseCurrencyCode), cancellationToken);

    [McpServerTool(Name = "delete_account"), Description(
        """
        Removes an account via soft delete (sets `IsDeleted`/`DeletedAt`; the row is never
        physically removed and disappears from all default queries afterwards).

        ## Parameters
        - **accountId** — `Guid` of the account to remove. Required.
        - **confirm** — Must be explicitly `true` for the operation to proceed. Required.

        ## DESTRUCTIVE OPERATION
        Always confirm explicitly with the user before calling this tool with `confirm = true`.
        Calling it with `confirm = false` (or omitted) is rejected by validation before any
        data is touched.

        ## Behavior
        - Throws a not-found error if `accountId` doesn't match a registered, non-credit-card
          account (use `delete_credit_card` for credit cards).
        - Does not cascade to linked transactions or credit cards — they keep their
          `accountId`/`paymentAccountId` references; consider that before deleting an
          account still in active use.

        ## Example
        ```json
        { "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "confirm": true }
        ```

        ## Returns
        A confirmation message string ("Conta removida (soft delete).").
        """)]
    public async Task<string> DeleteAccountAsync(Guid accountId, bool confirm, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DeleteAccountCommand(accountId, confirm), cancellationToken);
        return "Conta removida (soft delete).";
    }
}
