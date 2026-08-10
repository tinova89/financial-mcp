namespace FinancialMcp.Application.CreditCards.CreateCreditCard;

/// <summary>Response DTO — never expose the CreditCard domain entity directly (see CLAUDE.md > DTOs).</summary>
public sealed record CreditCardDto(
    Guid Id,
    string DisplayName,
    string BankCode,
    decimal InitialAmount,
    string Kind,
    string BaseCurrencyCode,
    byte ClosingDay,
    byte DueDay,
    Guid PaymentAccountId);
