namespace FinancialMcp.Application.Accounts.CreateAccount;

/// <summary>Response DTO — never expose the Account domain entity directly (see CLAUDE.md > DTOs).</summary>
public sealed record CheckingAccountDto(
    Guid Id,
    string DisplayName,
    string BankCode,
    decimal InitialAmount,
    string Kind,
    string BaseCurrencyCode,
    IReadOnlyList<Guid> CreditCardIds);
