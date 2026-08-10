using System.ComponentModel;
using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Accounts.DeleteAccount;
using FinancialMcp.Application.Accounts.GetAccount;
using FinancialMcp.Application.Accounts.ListAccounts;
using FinancialMcp.Application.Accounts.UpdateAccount;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

/// <summary>
/// MCP tools for checking accounts (bank accounts that Transactions and Cards link to).
/// Each tool is "thin": it only builds the MediatR request and calls IMediator.Send —
/// all business logic lives in the handlers in FinancialMcp.Application (see CLAUDE.md > Mediator Pattern).
/// </summary>
[McpServerToolType]
public sealed class AccountTools(IMediator mediator)
{
    [McpServerTool(Name = "list_accounts"), Description("Lists all registered checking accounts.")]
    public Task<IReadOnlyList<AccountDto>> ListAccountsAsync(CancellationToken cancellationToken = default) =>
        mediator.Send(new ListAccountsQuery(), cancellationToken);

    [McpServerTool(Name = "get_account"), Description("Detail of a specific checking account.")]
    public Task<AccountDto> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetAccountQuery(accountId), cancellationToken);

    [McpServerTool(Name = "create_account"), Description(
        "Registers a new checking account (bank), used to link transactions and cards.")]
    public Task<AccountDto> CreateAccountAsync(string name, string bank, CancellationToken cancellationToken = default) =>
        mediator.Send(new CreateAccountCommand(name, bank), cancellationToken);

    [McpServerTool(Name = "update_account"), Description("Changes fields (name, bank) of an existing checking account.")]
    public Task<AccountDto> UpdateAccountAsync(
        Guid accountId, string? name = null, string? bank = null, CancellationToken cancellationToken = default) =>
        mediator.Send(new UpdateAccountCommand(accountId, name, bank), cancellationToken);

    [McpServerTool(Name = "delete_account"), Description(
        "Removes (soft delete) a checking account. DESTRUCTIVE OPERATION: requires confirm = true. " +
        "Always confirm explicitly with the user before calling this tool with confirm = true.")]
    public async Task<string> DeleteAccountAsync(Guid accountId, bool confirm, CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DeleteAccountCommand(accountId, confirm), cancellationToken);
        return "Conta removida (soft delete).";
    }
}
