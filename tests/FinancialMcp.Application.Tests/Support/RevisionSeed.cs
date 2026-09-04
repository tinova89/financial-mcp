using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Infrastructure.Persistence;

namespace FinancialMcp.Application.Tests.Support;

/// <summary>Shared entity-graph builders reused across the Application handler/validator tests.</summary>
internal static class RevisionSeed
{
    public static Account NewAccount(string name = "Nubank") => new()
    {
        DisplayName = name,
        BankCode = "260",
        Group = "HOME",
        BaseCurrencyCode = "BRL",
        InitialAmount = 0m,
    };

    public static CreditCard NewCreditCard(string name, Account paymentAccount, byte closingDay = 5, byte dueDay = 15) => new()
    {
        DisplayName = name,
        BankCode = "260",
        Group = "HOME",
        BaseCurrencyCode = "BRL",
        InitialAmount = 0m,
        ClosingDay = closingDay,
        DueDay = dueDay,
        PaymentAccountId = paymentAccount.Id,
        PaymentAccount = paymentAccount,
    };

    public static TransactionCategory NewCategory(string name = "Moradia") => new() { Name = name };

    public static BudgetGoal NewBudgetGoal(
        TransactionCategory parentCategory,
        BudgetPeriodType period,
        int year,
        int month,
        decimal goalAmount,
        string currencyCode = "BRL") => new()
    {
        RawCategoryId = parentCategory.Id,
        RawCategory = parentCategory,
        Period = period,
        Year = year,
        Month = month,
        GoalAmount = goalAmount,
        CurrencyCode = currencyCode,
    };

    public static Transaction NewParentTransaction(Account account, TransactionCategory category) => new()
    {
        Type = TransactionType.Expense,
        Status = TransactionStatus.Revision,
        Description = "parent",
        Amount = -1m,
        Account = account,
        Category = category,
        ExpectedDate = new DateOnly(2026, 1, 1),
    };

    public static TransactionRevision NewRevision(
        Transaction parent,
        Account account,
        TransactionCategory category,
        string description,
        DateTimeOffset createdAt,
        decimal amount = -123.45m) => new()
    {
        Transaction = parent,
        Account = account,
        Category = category,
        Type = TransactionType.Expense,
        Status = TransactionStatus.Revision,
        Description = description,
        Amount = amount,
        ExpectedDate = new DateOnly(2026, 2, 10),
        Recurrence = RecurrenceType.None,
        CreatedAt = createdAt,
    };

    /// <summary>Seeds one account + category + parent transaction; returns them tracked-and-saved.</summary>
    public static async Task<(Account Account, TransactionCategory Category, Transaction Parent)> SeedGraphAsync(
        ApplicationDbContext context)
    {
        var account = NewAccount();
        var category = NewCategory();
        var parent = NewParentTransaction(account, category);

        context.Accounts.Add(account);
        context.TransactionCategories.Add(category);
        context.Transactions.Add(parent);
        await context.SaveChangesAsync();

        return (account, category, parent);
    }
}
