namespace FinancialMcp.Application.Accounts.CreateAccount;

/// <summary>Response DTO — never expose the Account domain entity directly (see CLAUDE.md > DTOs).</summary>
public sealed record AccountDto(
    Guid Id,
    string Name,
    string Bank,
    IReadOnlyList<Guid> CardIds);
