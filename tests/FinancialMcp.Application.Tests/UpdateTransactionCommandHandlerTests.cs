using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Application.Transactions.UpdateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `update_transaction`'s partial-patch semantics, the "stamp once"
/// <c>TransitionTo</c> rule across two sequential status changes, and post-write
/// RemainingBudget/RemainingBudgetPercentage recalculation. Uses a real
/// <see cref="SqliteInMemoryDatabase"/> — the handler loads and mutates a tracked entity
/// in place, which a hand-wired mocked DbSet would only reproduce awkwardly.
/// </summary>
public class UpdateTransactionCommandHandlerTests
{
    private static Transaction NewTransaction(Account account, TransactionCategory category) => new()
    {
        Type = TransactionType.Expense,
        Status = TransactionStatus.Revision,
        Description = "Compra original",
        Amount = -20m,
        Account = account,
        Category = category,
        ExpectedDate = new DateOnly(2026, 4, 1),
        Recurrence = RecurrenceType.None,
    };

    private static ICategoryBudgetRemainingCalculator StubCalculator(decimal? remaining = 10m, decimal? percentage = 0.1m)
    {
        var calculator = Substitute.For<ICategoryBudgetRemainingCalculator>();
        calculator.CalculateAsync(Arg.Any<Transaction>(), true, Arg.Any<CancellationToken>())
            .Returns(new CategoryBudgetRemaining(remaining, percentage));
        return calculator;
    }

    private static ITransactionCategoryResolver StubResolver(TransactionCategory? resolvedTo = null)
    {
        var resolver = Substitute.For<ITransactionCategoryResolver>();
        if (resolvedTo is not null)
        {
            resolver.When(x => x.ResolveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>()))
                .Do(ci => ci.Arg<Transaction>().Category = resolvedTo);
        }
        return resolver;
    }

    [Fact]
    public async Task Partial_patch_of_amount_only_leaves_other_fields_unchanged()
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
        var handler = new UpdateTransactionCommandHandler(ctx, StubResolver(), StubCalculator());

        var dto = await handler.Handle(new UpdateTransactionCommand(transactionId, Amount: -99m), CancellationToken.None);

        dto.Amount.Should().Be(-99m);
        dto.Status.Should().Be(nameof(TransactionStatus.Revision));
        dto.RawCategory.Should().Be("Moradia");
        dto.ExpectedDate.Should().Be(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public async Task Transitioning_to_scheduled_stamps_scheduled_at_once()
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
            var handler = new UpdateTransactionCommandHandler(ctx, StubResolver(), StubCalculator());
            await handler.Handle(new UpdateTransactionCommand(transactionId, Status: TransactionStatus.Scheduled), CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        await using var assert = db.NewContext();
        var stored = await assert.Transactions.SingleAsync(t => t.Id == transactionId);
        stored.Status.Should().Be(TransactionStatus.Scheduled);
        stored.ScheduledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task A_second_status_transition_does_not_re_stamp_the_first_timestamp()
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
            var handler = new UpdateTransactionCommandHandler(ctx, StubResolver(), StubCalculator());
            await handler.Handle(new UpdateTransactionCommand(transactionId, Status: TransactionStatus.Scheduled), CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        DateTimeOffset scheduledAtAfterFirstPatch;
        await using (var read = db.NewContext())
        {
            scheduledAtAfterFirstPatch = (await read.Transactions.SingleAsync(t => t.Id == transactionId)).ScheduledAt!.Value;
        }

        await Task.Delay(10); // ensure a later call's "now" would differ if it were (wrongly) re-stamped

        await using (var ctx = db.NewContext())
        {
            var handler = new UpdateTransactionCommandHandler(ctx, StubResolver(), StubCalculator());
            await handler.Handle(new UpdateTransactionCommand(transactionId, Status: TransactionStatus.Confirmed), CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        await using var assert = db.NewContext();
        var stored = await assert.Transactions.SingleAsync(t => t.Id == transactionId);
        stored.Status.Should().Be(TransactionStatus.Confirmed);
        stored.ConfirmedAt.Should().NotBeNull();
        stored.ScheduledAt.Should().Be(scheduledAtAfterFirstPatch, "ScheduledAt is stamped once and never overwritten by a later transition");
    }

    [Fact]
    public async Task Recalculates_remaining_budget_after_an_amount_change_including_the_transaction()
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
        var calculator = StubCalculator(remaining: 77.70m, percentage: 0.5m);
        var handler = new UpdateTransactionCommandHandler(ctx, StubResolver(), calculator);

        var dto = await handler.Handle(new UpdateTransactionCommand(transactionId, Amount: -33m), CancellationToken.None);

        dto.RemainingBudget.Should().Be(77.70m);
        dto.RemainingBudgetPercentage.Should().Be(0.5m);
        await calculator.Received(1).CalculateAsync(Arg.Any<Transaction>(), includeTransaction: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recalculates_remaining_budget_after_a_raw_category_change_and_re_resolves_category()
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
        var newCategory = new TransactionCategory { Name = "Lazer" };
        var resolver = StubResolver(resolvedTo: newCategory);
        var calculator = StubCalculator();
        var handler = new UpdateTransactionCommandHandler(ctx, resolver, calculator);

        var dto = await handler.Handle(new UpdateTransactionCommand(transactionId, RawCategory: "Lazer"), CancellationToken.None);

        dto.RawCategory.Should().Be("Lazer");
        await resolver.Received(1).ResolveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await calculator.Received(1).CalculateAsync(Arg.Any<Transaction>(), includeTransaction: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_not_found_for_an_unknown_transaction_id()
    {
        using var db = new SqliteInMemoryDatabase();
        await using var ctx = db.NewContext();
        var handler = new UpdateTransactionCommandHandler(ctx, StubResolver(), StubCalculator());

        var act = async () => await handler.Handle(new UpdateTransactionCommand(Guid.NewGuid(), Amount: -1m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Command_has_no_account_id_recurrence_or_installment_properties()
    {
        // AccountId/Recurrence/CurrentInstallment/TotalInstallments cannot be patched via
        // update_transaction because UpdateTransactionCommand structurally has no such
        // properties — this documents that contract rather than testing a runtime rejection.
        var properties = typeof(UpdateTransactionCommand).GetProperties().Select(p => p.Name);

        properties.Should().BeEquivalentTo(
            "TransactionId", "Status", "RawCategory", "Amount", "ExpectedDate", "ActualDate", "ConfirmationDate", "InvoiceDueDate");
    }
}
