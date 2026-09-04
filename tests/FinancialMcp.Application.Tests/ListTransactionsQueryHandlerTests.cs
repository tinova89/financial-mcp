using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Application.Transactions.ListTransactions;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `list_transactions` filtering, pagination, ordering, and `needsConfirmation`.
///
/// Subcategory/Year/Month filters run in memory <em>after</em> the SQL-level Skip/Take, so
/// <c>TotalCount</c> (computed before those filters) can exceed <c>Items.Count</c> when they
/// narrow an already-paged page — this is documented, existing handler behavior, not a bug
/// under repair (see <see cref="TotalCount_can_exceed_items_count_when_a_post_page_filter_narrows_the_page"/>).
/// </summary>
public class ListTransactionsQueryHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly DateOnly WidePeriodStart = Today.AddYears(-2);
    private static readonly DateOnly WidePeriodEnd = Today.AddYears(2);

    private static Transaction NewTransaction(
        Account account,
        TransactionCategory category,
        DateOnly expectedDate,
        TransactionType type = TransactionType.Expense,
        TransactionStatus status = TransactionStatus.Scheduled,
        DateOnly? confirmationDate = null,
        DateOnly? invoiceDueDate = null) => new()
    {
        Type = type,
        Status = status,
        Description = "tx",
        Amount = -10m,
        Account = account,
        AccountId = account.Id,
        Category = category,
        ExpectedDate = expectedDate,
        ConfirmationDate = confirmationDate,
        InvoiceDueDate = invoiceDueDate,
        Recurrence = RecurrenceType.None,
    };

    private static IApplicationDbContext DbWith(params Transaction[] transactions)
    {
        // Build the mock DbSet before touching the substitute — BuildMockDbSet() configures
        // its own NSubstitute internally and would otherwise clobber the pending Returns() call.
        var set = transactions.ToList().BuildMockDbSet();
        var db = Substitute.For<IApplicationDbContext>();
        db.Transactions.Returns(set);
        return db;
    }

    private static ListTransactionsQuery Query(
        TransactionType? type = null,
        TransactionStatus? status = null,
        string? parentCategory = null,
        string? subcategory = null,
        Guid? accountId = null,
        int? year = null,
        int? month = null,
        int page = 1,
        int pageSize = 50) => new(
            WidePeriodStart, WidePeriodEnd, type, status, parentCategory, subcategory, accountId, year, month, page, pageSize);

    [Fact]
    public async Task Filters_by_type()
    {
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory();
        var expense = NewTransaction(account, category, Today, type: TransactionType.Expense);
        var income = NewTransaction(account, category, Today, type: TransactionType.Income);

        var result = await new ListTransactionsQueryHandler(DbWith(expense, income))
            .Handle(Query(type: TransactionType.Expense), CancellationToken.None);

        result.Items.Should().ContainSingle(t => t.Id == expense.Id);
    }

    [Fact]
    public async Task Filters_by_status()
    {
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory();
        var scheduled = NewTransaction(account, category, Today, status: TransactionStatus.Scheduled);
        var confirmed = NewTransaction(account, category, Today, status: TransactionStatus.Confirmed, confirmationDate: Today);

        var result = await new ListTransactionsQueryHandler(DbWith(scheduled, confirmed))
            .Handle(Query(status: TransactionStatus.Confirmed), CancellationToken.None);

        result.Items.Should().ContainSingle(t => t.Id == confirmed.Id);
    }

    [Fact]
    public async Task Filters_by_account_id()
    {
        var accountA = RevisionSeed.NewAccount("A");
        var accountB = RevisionSeed.NewAccount("B");
        var category = RevisionSeed.NewCategory();
        var txA = NewTransaction(accountA, category, Today);
        var txB = NewTransaction(accountB, category, Today);

        var result = await new ListTransactionsQueryHandler(DbWith(txA, txB))
            .Handle(Query(accountId: accountA.Id), CancellationToken.None);

        result.Items.Should().ContainSingle(t => t.Id == txA.Id);
    }

    [Fact]
    public async Task Filters_by_mandatory_period_range()
    {
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory();
        var inside = NewTransaction(account, category, Today);
        var outside = NewTransaction(account, category, Today.AddYears(-5));

        var result = await new ListTransactionsQueryHandler(DbWith(inside, outside))
            .Handle(Query(), CancellationToken.None);

        result.Items.Should().ContainSingle(t => t.Id == inside.Id);
    }

    [Fact]
    public async Task Filters_by_parent_category_matching_either_the_category_itself_or_a_subcategory_under_it()
    {
        var account = RevisionSeed.NewAccount();
        var moradia = new TransactionCategory { Name = "Moradia" };
        var aluguel = new TransactionCategory { Name = "Aluguel", ParentCategoryId = moradia.Id, ParentCategory = moradia };
        var lazer = new TransactionCategory { Name = "Lazer" };

        var underParent = NewTransaction(account, moradia, Today);
        var underSubcategory = NewTransaction(account, aluguel, Today);
        var unrelated = NewTransaction(account, lazer, Today);

        var result = await new ListTransactionsQueryHandler(DbWith(underParent, underSubcategory, unrelated))
            .Handle(Query(parentCategory: "Moradia"), CancellationToken.None);

        result.Items.Select(t => t.Id).Should().BeEquivalentTo([underParent.Id, underSubcategory.Id]);
    }

    [Fact]
    public async Task Filters_by_subcategory_exact_match()
    {
        var account = RevisionSeed.NewAccount();
        var moradia = new TransactionCategory { Name = "Moradia" };
        var aluguel = new TransactionCategory { Name = "Aluguel", ParentCategoryId = moradia.Id, ParentCategory = moradia };
        var seguro = new TransactionCategory { Name = "Seguro", ParentCategoryId = moradia.Id, ParentCategory = moradia };

        var underAluguel = NewTransaction(account, aluguel, Today);
        var underSeguro = NewTransaction(account, seguro, Today);

        var result = await new ListTransactionsQueryHandler(DbWith(underAluguel, underSeguro))
            .Handle(Query(subcategory: "Aluguel"), CancellationToken.None);

        result.Items.Should().ContainSingle(t => t.Id == underAluguel.Id);
    }

    [Fact]
    public async Task Filters_by_year_and_month_via_reference_month_year()
    {
        var checkingAccount = RevisionSeed.NewAccount("Checking");
        var creditCard = RevisionSeed.NewCreditCard("Card", RevisionSeed.NewAccount("Payer"));
        var category = RevisionSeed.NewCategory();

        var checkingInMonth = NewTransaction(
            checkingAccount, category, Today, status: TransactionStatus.Confirmed, confirmationDate: new DateOnly(2026, 6, 15));
        var checkingOutOfMonth = NewTransaction(
            checkingAccount, category, Today, status: TransactionStatus.Confirmed, confirmationDate: new DateOnly(2026, 7, 15));
        var creditInMonth = NewTransaction(
            creditCard, category, Today, invoiceDueDate: new DateOnly(2026, 6, 20));
        var creditOutOfMonth = NewTransaction(
            creditCard, category, Today, invoiceDueDate: new DateOnly(2026, 7, 20));

        var result = await new ListTransactionsQueryHandler(
                DbWith(checkingInMonth, checkingOutOfMonth, creditInMonth, creditOutOfMonth))
            .Handle(Query(year: 2026, month: 6), CancellationToken.None);

        result.Items.Select(t => t.Id).Should().BeEquivalentTo([checkingInMonth.Id, creditInMonth.Id]);
    }

    [Fact]
    public async Task Orders_by_expected_date_descending()
    {
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory();
        var oldest = NewTransaction(account, category, Today.AddDays(-2));
        var newest = NewTransaction(account, category, Today);
        var middle = NewTransaction(account, category, Today.AddDays(-1));

        var result = await new ListTransactionsQueryHandler(DbWith(oldest, newest, middle))
            .Handle(Query(), CancellationToken.None);

        result.Items.Select(t => t.Id).Should().Equal(newest.Id, middle.Id, oldest.Id);
    }

    [Fact]
    public async Task Paginates_using_page_and_page_size()
    {
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory();
        var t1 = NewTransaction(account, category, Today.AddDays(-1));
        var t2 = NewTransaction(account, category, Today.AddDays(-2));
        var t3 = NewTransaction(account, category, Today.AddDays(-3));

        var page2 = await new ListTransactionsQueryHandler(DbWith(t1, t2, t3))
            .Handle(Query(page: 2, pageSize: 2), CancellationToken.None);

        page2.TotalCount.Should().Be(3);
        page2.Items.Select(t => t.Id).Should().Equal(t3.Id);
    }

    [Fact]
    public async Task Computes_needs_confirmation_per_row_relative_to_today()
    {
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory();
        var scheduledPast = NewTransaction(account, category, Today.AddDays(-1), status: TransactionStatus.Scheduled);
        var scheduledFuture = NewTransaction(account, category, Today.AddDays(1), status: TransactionStatus.Scheduled);
        var confirmedPast = NewTransaction(
            account, category, Today.AddDays(-1), status: TransactionStatus.Confirmed, confirmationDate: Today.AddDays(-1));

        var result = await new ListTransactionsQueryHandler(DbWith(scheduledPast, scheduledFuture, confirmedPast))
            .Handle(Query(), CancellationToken.None);

        result.Items.Single(t => t.Id == scheduledPast.Id).NeedsConfirmation.Should().BeTrue();
        result.Items.Single(t => t.Id == scheduledFuture.Id).NeedsConfirmation.Should().BeFalse();
        result.Items.Single(t => t.Id == confirmedPast.Id).NeedsConfirmation.Should().BeFalse();
    }

    [Fact]
    public async Task TotalCount_can_exceed_items_count_when_a_post_page_filter_narrows_the_page()
    {
        // Locked-in current-behavior test — not a bug fix. Both rows match every SQL-level
        // filter (Type/Status/AccountId/Period), so TotalCount counts both; the Subcategory
        // filter only runs afterward, in memory, against the already-paged page.
        var account = RevisionSeed.NewAccount();
        var moradia = new TransactionCategory { Name = "Moradia" };
        var aluguel = new TransactionCategory { Name = "Aluguel", ParentCategoryId = moradia.Id, ParentCategory = moradia };

        var underParentOnly = NewTransaction(account, moradia, Today);
        var underAluguel = NewTransaction(account, aluguel, Today.AddDays(-1));

        var result = await new ListTransactionsQueryHandler(DbWith(underParentOnly, underAluguel))
            .Handle(Query(subcategory: "Aluguel", page: 1, pageSize: 2), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().ContainSingle(t => t.Id == underAluguel.Id);
        result.TotalCount.Should().BeGreaterThan(result.Items.Count);
    }
}
