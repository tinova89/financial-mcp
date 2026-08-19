using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Accounts.UpdateAccount;

/// <summary>
/// Changes fields of an existing account. Corresponds to the MCP tool `update_account`.
/// Null fields are ignored (partial patch). Kind cannot be patched — it's computed
/// from the entity's own type (see Account.Kind).
/// </summary>
public sealed record UpdateCheckingAccountCommand(
    Guid AccountId,
    string? DisplayName = null,
    string? BankCode = null,
    decimal? InitialAmount = null,
    string? BaseCurrencyCode = null) : IRequest<CheckingAccountDto>, ITransactionalRequest;
