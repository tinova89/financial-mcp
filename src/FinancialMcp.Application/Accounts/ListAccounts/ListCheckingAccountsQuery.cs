using FinancialMcp.Application.Accounts.CreateAccount;
using MediatR;

namespace FinancialMcp.Application.Accounts.ListAccounts;

/// <summary>Lists all registered checking accounts. Corresponds to the MCP tool `list_accounts`.</summary>
public sealed record ListCheckingAccountsQuery : IRequest<IReadOnlyList<CheckingAccountDto>>;
