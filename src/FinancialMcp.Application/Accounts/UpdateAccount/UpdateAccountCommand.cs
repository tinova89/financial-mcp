using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Accounts.UpdateAccount;

/// <summary>
/// Changes fields of an existing account. Corresponds to the MCP tool `update_account`.
/// Null fields are ignored (partial patch).
/// </summary>
public sealed record UpdateAccountCommand(
    Guid AccountId,
    string? Name = null,
    string? Bank = null) : IRequest<AccountDto>, ITransactionalRequest;
