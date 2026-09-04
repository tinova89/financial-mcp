using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Application.Transactions.DeleteTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `delete_transaction`'s handler: soft delete persistence, exclusion from
/// subsequent reads via the global query filter, and post-delete RemainingBudget/
/// RemainingBudgetPercentage recalculation (excluding the transaction itself). The
/// <c>Confirm</c> guard has no equivalent check in the handler — it is enforced entirely by
/// <see cref="DeleteTransactionCommandValidator"/> via <c>ValidationBehavior</c>, before the
/// handler ever runs — see <see cref="DeleteTransactionCommandValidatorTests"/> for that case.
/// </summary>
public class DeleteTransactionCommandHandlerTests
{
    private static Transaction NewTransaction(Account account, TransactionCategory category) => new()
    {
        Type = TransactionType.Expense,
        Status = TransactionStatus.Scheduled,
        Description = "Compra a apagar",
        Amount = -15m,
        Account = account,
        Category = category,
        ExpectedDate = new DateOnly(2026, 5, 1),
        Recurrence = RecurrenceType.None,
    };

    private static ICategoryBudgetRemainingCalculator StubCalculator(decimal? remaining = 5m, decimal? percentage = 0.05m)
    {
        var calculator = Substitute.For<ICategoryBudgetRemainingCalculator>();
        calculator.CalculateAsync(Arg.Any<Transaction>(), false, Arg.Any<CancellationToken>())
            .Returns(new CategoryBudgetRemaining(remaining, percentage));
        return calculator;
    }

    [Fact]
    public async Task Marks_the_transaction_deleted_and_persists_is_deleted_and_deleted_at()
    {
        using var db = new SqliteInMemoryDatabase();
        Guid transactionId;

        await using (var seed = db.NewContext())
        {
            var (account, category, _) = await RevisionSeed.SeedGraphAsync(seed);
            var tx = NewTransaction(account, category);
            seed.Transactions.Add(tx);
            await seed.SaveChangesAsync();
            transactionId = tx.Id;
        }

        await using (var ctx = db.NewContext())
        {
            var handler = new DeleteTransactionCommandHandler(ctx, StubCalculator());
            await handler.Handle(new DeleteTransactionCommand(transactionId, Confirm: true), CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        await using var assert = db.NewContext();
        var stored = await assert.Transactions.IgnoreQueryFilters().SingleAsync(t => t.Id == transactionId);
        stored.IsDeleted.Should().BeTrue();
        stored.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task A_deleted_transaction_is_excluded_from_subsequent_default_reads()
    {
        using var db = new SqliteInMemoryDatabase();
        Guid transactionId;

        await using (var seed = db.NewContext())
        {
            var (account, category, _) = await RevisionSeed.SeedGraphAsync(seed);
            var tx = NewTransaction(account, category);
            seed.Transactions.Add(tx);
            await seed.SaveChangesAsync();
            transactionId = tx.Id;
        }

        await using (var ctx = db.NewContext())
        {
            var handler = new DeleteTransactionCommandHandler(ctx, StubCalculator());
            await handler.Handle(new DeleteTransactionCommand(transactionId, Confirm: true), CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        await using var assert = db.NewContext();
        var found = await assert.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId);
        found.Should().BeNull("the global soft-delete query filter excludes it by default");
    }

    [Fact]
    public async Task Recalculates_remaining_budget_excluding_the_transaction_itself()
    {
        using var db = new SqliteInMemoryDatabase();
        Guid transactionId;

        await using (var seed = db.NewContext())
        {
            var (account, category, _) = await RevisionSeed.SeedGraphAsync(seed);
            var tx = NewTransaction(account, category);
            seed.Transactions.Add(tx);
            await seed.SaveChangesAsync();
            transactionId = tx.Id;
        }

        await using var ctx = db.NewContext();
        var calculator = StubCalculator(remaining: 200m, percentage: 0.8m);
        var handler = new DeleteTransactionCommandHandler(ctx, calculator);

        var dto = await handler.Handle(new DeleteTransactionCommand(transactionId, Confirm: true), CancellationToken.None);

        dto.RemainingBudget.Should().Be(200m);
        dto.RemainingBudgetPercentage.Should().Be(0.8m);
        await calculator.Received(1).CalculateAsync(Arg.Any<Transaction>(), includeTransaction: false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_not_found_for_an_unknown_transaction_id()
    {
        using var db = new SqliteInMemoryDatabase();
        await using var ctx = db.NewContext();
        var handler = new DeleteTransactionCommandHandler(ctx, StubCalculator());

        var act = async () => await handler.Handle(new DeleteTransactionCommand(Guid.NewGuid(), Confirm: true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
