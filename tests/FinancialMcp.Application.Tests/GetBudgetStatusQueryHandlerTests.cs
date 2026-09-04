using FinancialMcp.Application.BudgetGoals.GetBudgetStatus;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `get_budget_status`: BudgetGoal.ResolveEffective precedence (Monthly carries
/// forward, OneTime beats an applicable Monthly goal for its own target month), the
/// per-account-kind reference month (ConfirmationDate for checking, InvoiceDueDate for
/// credit card), the RemainingBudget/PercentUsed formulas, and exclusion of categories with
/// no effective goal.
///
/// Doc-comment flag (not a test): CLAUDE.md's Business Rules claim credit-card InvoiceDueDate
/// rolls to the next business day when it falls on a weekend, but this handler never calls
/// BusinessDayHelper — it's dead/unwired code. These tests assert only the actual current
/// (no-rolling) behavior; the doc/code mismatch is left as a follow-up decision, per the
/// user's choice not to touch CLAUDE.md's Business Rules text for this tests-only card.
/// </summary>
public class GetBudgetStatusQueryHandlerTests
{
    private static Transaction NewExpense(
        Account account,
        TransactionCategory category,
        DateOnly expectedDate,
        TransactionStatus status = TransactionStatus.Confirmed,
        TransactionType type = TransactionType.Expense,
        DateOnly? confirmationDate = null,
        DateOnly? invoiceDueDate = null,
        decimal amount = -100m) => new()
    {
        Type = type,
        Status = status,
        Description = "expense",
        Amount = amount,
        Account = account,
        AccountId = account.Id,
        Category = category,
        ExpectedDate = expectedDate,
        ConfirmationDate = confirmationDate,
        InvoiceDueDate = invoiceDueDate,
        Recurrence = RecurrenceType.None,
    };

    [Fact]
    public async Task Monthly_goal_applies_from_its_own_month_onward_when_no_later_goal_supersedes()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory("Moradia");
        var goal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.Monthly, 2026, 3, 500m);
        var expense = NewExpense(account, category, new DateOnly(2026, 5, 10), confirmationDate: new DateOnly(2026, 5, 10), amount: -100m);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            seed.BudgetGoals.Add(goal);
            seed.Transactions.Add(expense);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 5), CancellationToken.None);

        var dto = result.Should().ContainSingle(d => d.RawCategory == "Moradia").Subject;
        dto.GoalAmount.Should().Be(500m);
        dto.ActualSpend.Should().Be(100m);
    }

    [Fact]
    public async Task Monthly_goal_is_superseded_by_a_later_monthly_goal_for_the_same_category()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory("Moradia");
        var earlierGoal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.Monthly, 2026, 3, 500m);
        var laterGoal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.Monthly, 2026, 4, 700m);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            seed.BudgetGoals.AddRange(earlierGoal, laterGoal);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 5), CancellationToken.None);

        result.Should().ContainSingle(d => d.RawCategory == "Moradia").Which.GoalAmount.Should().Be(700m);
    }

    [Fact]
    public async Task One_time_goal_matches_only_its_exact_year_and_month()
    {
        using var db = new SqliteInMemoryDatabase();
        var category = RevisionSeed.NewCategory("Moradia");
        var goal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.OneTime, 2026, 6, 1000m);

        await using (var seed = db.NewContext())
        {
            seed.TransactionCategories.Add(category);
            seed.BudgetGoals.Add(goal);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();

        var matchingMonth = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 6), CancellationToken.None);
        matchingMonth.Should().ContainSingle(d => d.RawCategory == "Moradia").Which.GoalAmount.Should().Be(1000m);

        var adjacentMonth = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 7), CancellationToken.None);
        adjacentMonth.Should().BeEmpty("a OneTime goal has no fallback to an adjacent month");
    }

    [Fact]
    public async Task One_time_goal_wins_over_an_applicable_monthly_goal_for_the_same_target_month()
    {
        using var db = new SqliteInMemoryDatabase();
        var category = RevisionSeed.NewCategory("Moradia");
        var monthlyGoal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.Monthly, 2026, 1, 300m);
        var oneTimeGoal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.OneTime, 2026, 6, 1000m);

        await using (var seed = db.NewContext())
        {
            seed.TransactionCategories.Add(category);
            seed.BudgetGoals.AddRange(monthlyGoal, oneTimeGoal);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 6), CancellationToken.None);

        result.Should().ContainSingle(d => d.RawCategory == "Moradia").Which.GoalAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task Reference_month_uses_confirmation_date_for_checking_accounts_and_invoice_due_date_for_credit_cards()
    {
        using var db = new SqliteInMemoryDatabase();
        var checkingAccount = RevisionSeed.NewAccount("Checking");
        var payer = RevisionSeed.NewAccount("Payer");
        var creditCard = RevisionSeed.NewCreditCard("Card", payer);
        var category = RevisionSeed.NewCategory("Moradia");
        var goal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.Monthly, 2026, 1, 10000m);

        var checkingInMonth = NewExpense(checkingAccount, category, new DateOnly(2026, 6, 1), confirmationDate: new DateOnly(2026, 6, 15), amount: -100m);
        var checkingOutOfMonth = NewExpense(checkingAccount, category, new DateOnly(2026, 7, 1), confirmationDate: new DateOnly(2026, 7, 15), amount: -900m);
        var creditInMonth = NewExpense(creditCard, category, new DateOnly(2026, 6, 1), invoiceDueDate: new DateOnly(2026, 6, 20), amount: -50m);
        var creditOutOfMonth = NewExpense(creditCard, category, new DateOnly(2026, 7, 1), invoiceDueDate: new DateOnly(2026, 7, 20), amount: -900m);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.AddRange(checkingAccount, payer);
            seed.CreditCards.Add(creditCard);
            seed.TransactionCategories.Add(category);
            seed.BudgetGoals.Add(goal);
            seed.Transactions.AddRange(checkingInMonth, checkingOutOfMonth, creditInMonth, creditOutOfMonth);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 6), CancellationToken.None);

        result.Should().ContainSingle(d => d.RawCategory == "Moradia").Which.ActualSpend.Should().Be(150m);
    }

    [Fact]
    public async Task Remaining_budget_and_percent_used_formulas()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory("Moradia");
        var goal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.Monthly, 2026, 1, 500m);
        var e1 = NewExpense(account, category, new DateOnly(2026, 5, 1), confirmationDate: new DateOnly(2026, 5, 1), amount: -150m);
        var e2 = NewExpense(account, category, new DateOnly(2026, 5, 2), confirmationDate: new DateOnly(2026, 5, 2), amount: -150m);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            seed.BudgetGoals.Add(goal);
            seed.Transactions.AddRange(e1, e2);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 5), CancellationToken.None);

        var dto = result.Should().ContainSingle(d => d.RawCategory == "Moradia").Subject;
        dto.ActualSpend.Should().Be(300m);
        dto.RemainingBudget.Should().Be(200m);
        dto.PercentUsed.Should().Be(0.6m);
    }

    [Fact]
    public async Task Percent_used_is_null_when_goal_amount_is_zero()
    {
        // Seeded directly, bypassing the create-path validator's GreaterThan(0) rule — this
        // exercises the query handler's own defensive branch, not a create_category_budget scenario.
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory("Moradia");
        var goal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.Monthly, 2026, 1, 0m);
        var expense = NewExpense(account, category, new DateOnly(2026, 5, 1), confirmationDate: new DateOnly(2026, 5, 1), amount: -50m);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            seed.BudgetGoals.Add(goal);
            seed.Transactions.Add(expense);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 5), CancellationToken.None);

        var dto = result.Should().ContainSingle(d => d.RawCategory == "Moradia").Subject;
        dto.PercentUsed.Should().BeNull();
        dto.RemainingBudget.Should().Be(-50m);
    }

    [Fact]
    public async Task A_category_with_transactions_but_no_effective_goal_is_excluded_entirely()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory("SemOrcamento");
        var expense = NewExpense(account, category, new DateOnly(2026, 5, 1), confirmationDate: new DateOnly(2026, 5, 1));

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            seed.Transactions.Add(expense);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 5), CancellationToken.None);

        result.Should().NotContain(d => d.RawCategory == "SemOrcamento");
    }

    [Fact]
    public async Task Non_expense_or_non_confirmed_transactions_never_contribute_to_actual_spend()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory("Moradia");
        var goal = RevisionSeed.NewBudgetGoal(category, BudgetPeriodType.Monthly, 2026, 1, 500m);

        var income = NewExpense(account, category, new DateOnly(2026, 5, 1), type: TransactionType.Income, confirmationDate: new DateOnly(2026, 5, 1), amount: -999m);
        var transfer = NewExpense(account, category, new DateOnly(2026, 5, 1), type: TransactionType.Transfer, confirmationDate: new DateOnly(2026, 5, 1), amount: -999m);
        var payment = NewExpense(account, category, new DateOnly(2026, 5, 1), type: TransactionType.Payment, confirmationDate: new DateOnly(2026, 5, 1), amount: -999m);
        var scheduledExpense = NewExpense(account, category, new DateOnly(2026, 5, 1), status: TransactionStatus.Scheduled, confirmationDate: new DateOnly(2026, 5, 1), amount: -999m);
        var revisionExpense = NewExpense(account, category, new DateOnly(2026, 5, 1), status: TransactionStatus.Revision, confirmationDate: new DateOnly(2026, 5, 1), amount: -999m);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            seed.BudgetGoals.Add(goal);
            seed.Transactions.AddRange(income, transfer, payment, scheduledExpense, revisionExpense);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new GetBudgetStatusQueryHandler(ctx).Handle(new GetBudgetStatusQuery(2026, 5), CancellationToken.None);

        result.Should().ContainSingle(d => d.RawCategory == "Moradia").Which.ActualSpend.Should().Be(0m);
    }
}
