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
}
