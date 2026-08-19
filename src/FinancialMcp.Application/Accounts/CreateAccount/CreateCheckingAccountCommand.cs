using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Accounts.CreateAccount;

/// <summary>
/// Registers a new checking account (bank), used to link Transactions and Cards.
/// Corresponds to the MCP tool `create_account` (see CLAUDE.md > MCP). Kind is not a
/// parameter — it's computed from the entity's own type (see Account.Kind).
/// </summary>
public sealed record CreateCheckingAccountCommand(
    string BankCode,
    string DisplayName,
    string BaseCurrencyCode,
    decimal InitialAmount
    ) : IRequest<CheckingAccountDto>, ITransactionalRequest;
