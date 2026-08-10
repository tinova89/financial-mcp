using FinancialApp.Model;
using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Accounts.CreateAccount;

/// <summary>
/// Registers a new checking account (bank), used to link Transactions and Cards.
/// Corresponds to the MCP tool `create_account` (see CLAUDE.md > MCP).
/// </summary>
public sealed record CreateAccountCommand(
    string BankCode,
    string DisplayName,
    string BaseCurrencyCode,
    decimal InitialAmount,
    FinancialAccountKind Kind
    ) : IRequest<AccountDto>, ITransactionalRequest;
