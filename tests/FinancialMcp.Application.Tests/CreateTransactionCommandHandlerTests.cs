using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `create_transaction`'s handler: per-status timestamp stamping, category
/// resolution, RemainingBudget/RemainingBudgetPercentage pass-through, and DTO mapping.
/// Required-field-per-statement-type <em>validator</em> rules are covered separately in
/// <c>CreateTransactionCommandValidatorTests</c>.
/// </summary>
public class CreateTransactionCommandHandlerTests
{
    private static readonly TransactionCategory ResolvedCategory = new() { Name = "Mercado" };

    private static (IApplicationDbContext Db, ITransactionCategoryResolver Resolver, ICategoryBudgetRemainingCalculator Calculator, Func<Transaction?> Captured)
        BuildDependencies(decimal? remainingBudget = 123.45m, decimal? remainingBudgetPercentage = 0.42m)
    {
        // Build the mock DbSet before touching the substitute — BuildMockDbSet() configures
        // its own NSubstitute internally and would otherwise clobber the pending Returns() call.
        var mockSet = new List<Transaction>().BuildMockDbSet();
        Transaction? captured = null;
        mockSet.When(x => x.Add(Arg.Any<Transaction>())).Do(ci => captured = ci.Arg<Transaction>());

        var db = Substitute.For<IApplicationDbContext>();
        db.Transactions.Returns(mockSet);

        var resolver = Substitute.For<ITransactionCategoryResolver>();
        resolver.When(x => x.ResolveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Transaction>().Category = ResolvedCategory);

        var calculator = Substitute.For<ICategoryBudgetRemainingCalculator>();
        calculator.CalculateAsync(Arg.Any<Transaction>(), true, Arg.Any<CancellationToken>())
            .Returns(new CategoryBudgetRemaining(remainingBudget, remainingBudgetPercentage));

        return (db, resolver, calculator, () => captured);
    }

    private static CreateTransactionCommand ValidCommand(
        TransactionStatus status = TransactionStatus.Scheduled,
        DateOnly? confirmationDate = null) => new(
            Type: TransactionType.Expense,
            Status: status,
            Description: "Compra no mercado",
            Amount: -50m,
            RawCategory: "Mercado",
            ExpectedDate: new DateOnly(2026, 3, 1),
            ActualDate: new DateOnly(2026, 3, 2),
            ConfirmationDate: confirmationDate,
            InvoiceDueDate: null,
            Recurrence: RecurrenceType.None,
            CurrentInstallment: null,
            TotalInstallments: null,
            AccountId: Guid.NewGuid());

    [Fact]
    public async Task Stamps_scheduled_at_not_confirmed_at_when_initial_status_is_scheduled()
    {
        var (db, resolver, calculator, captured) = BuildDependencies();
        var handler = new CreateTransactionCommandHandler(db, resolver, calculator);

        await handler.Handle(ValidCommand(status: TransactionStatus.Scheduled), CancellationToken.None);

        captured().Should().NotBeNull();
        captured()!.ScheduledAt.Should().NotBeNull();
        captured()!.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task Stamps_confirmed_at_when_initial_status_is_confirmed()
    {
        var (db, resolver, calculator, captured) = BuildDependencies();
        var handler = new CreateTransactionCommandHandler(db, resolver, calculator);

        await handler.Handle(
            ValidCommand(status: TransactionStatus.Confirmed, confirmationDate: new DateOnly(2026, 3, 1)),
            CancellationToken.None);

        captured().Should().NotBeNull();
        captured()!.ConfirmedAt.Should().NotBeNull();
        captured()!.ScheduledAt.Should().BeNull();
    }

    [Fact]
    public async Task Response_dto_carries_the_calculators_remaining_budget_values_verbatim()
    {
        var (db, resolver, calculator, _) = BuildDependencies(remainingBudget: 250.00m, remainingBudgetPercentage: 0.75m);
        var handler = new CreateTransactionCommandHandler(db, resolver, calculator);

        var dto = await handler.Handle(ValidCommand(), CancellationToken.None);

        dto.RemainingBudget.Should().Be(250.00m);
        dto.RemainingBudgetPercentage.Should().Be(0.75m);
    }

    [Fact]
    public async Task Maps_every_dto_field_one_to_one_from_the_entity_no_leakage()
    {
        var (db, resolver, calculator, captured) = BuildDependencies();
        var handler = new CreateTransactionCommandHandler(db, resolver, calculator);
        var command = ValidCommand();

        var dto = await handler.Handle(command, CancellationToken.None);
        var entity = captured()!;

        dto.Id.Should().Be(entity.Id);
        dto.Type.Should().Be(nameof(TransactionType.Expense));
        dto.Status.Should().Be(nameof(TransactionStatus.Scheduled));
        dto.Description.Should().Be(command.Description);
        dto.Amount.Should().Be(command.Amount);
        dto.RawCategory.Should().Be(ResolvedCategory.FullName, "RawCategory on the DTO comes from Category.FullName, not the transient RawCategory field");
        dto.ExpectedDate.Should().Be(command.ExpectedDate);
        dto.ActualDate.Should().Be(command.ActualDate);
        dto.AccountId.Should().Be(command.AccountId);
        dto.Recurrence.Should().Be(nameof(RecurrenceType.None));
    }

    [Fact]
    public async Task Calls_the_category_resolver_before_the_budget_calculator_reads_the_category()
    {
        var (db, resolver, calculator, _) = BuildDependencies();
        var handler = new CreateTransactionCommandHandler(db, resolver, calculator);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        await resolver.Received(1).ResolveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await calculator.Received(1).CalculateAsync(
            Arg.Is<Transaction>(t => t.Category == ResolvedCategory), true, Arg.Any<CancellationToken>());
    }
}
