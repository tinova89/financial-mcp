using FinancialMcp.Application.Accounts.CreateAccount;
using MediatR;

namespace FinancialMcp.Application.Accounts.GetAccount;

/// <summary>Detail of a specific account. Corresponds to the MCP tool `get_account`.</summary>
public sealed record GetCheckingAccountQuery(Guid AccountId) : IRequest<CheckingAccountDto>;
