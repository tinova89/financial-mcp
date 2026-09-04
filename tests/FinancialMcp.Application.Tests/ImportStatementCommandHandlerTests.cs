using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Statements.ImportStatement;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `import_statement`'s handler: Account.Kind-driven routing to the parser,
/// warnings/lines-imported pass-through, and atomic batch commit via TransactionBehavior.
/// The parser (<see cref="IStatementCsvParser"/>) is mocked here — real CSV-format edge
/// cases (malformed separator, bad date format) are covered directly against
/// <c>StatementCsvParser</c> in <c>StatementCsvParserTests</c>.
/// </summary>
public class ImportStatementCommandHandlerTests
{
    // Only CategoryId is set (never the Category navigation) — the "parsed" transaction is a
    // detached object handed in by the (mocked) parser, exactly as the real parser would
    // produce it before ITransactionCategoryResolver runs. Setting the navigation to an
    // entity instance already saved in a *different* DbContext would make EF Core treat it
    // as a new row to insert (cross-context entities aren't tracked as existing), causing a
    // duplicate-key failure on SaveChanges.
    private static Transaction StubParsedTransaction(Guid accountId, Guid categoryId) => new()
    {
        Type = TransactionType.Expense,
        Status = TransactionStatus.Scheduled,
        Description = "parsed row",
        Amount = -50m,
        AccountId = accountId,
        CategoryId = categoryId,
        ExpectedDate = new DateOnly(2026, 6, 1),
        Recurrence = RecurrenceType.None,
    };

    private static IStatementCsvParser StubParser(IReadOnlyList<Transaction> transactions, IReadOnlyList<string> warnings)
    {
        var parser = Substitute.For<IStatementCsvParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<Guid>(), out Arg.Any<IReadOnlyList<string>>())
            .Returns(ci =>
            {
                ci[3] = warnings;
                return transactions;
            });
        return parser;
    }

    private static ITransactionCategoryResolver NoOpResolver() => Substitute.For<ITransactionCategoryResolver>();

    [Fact]
    public async Task Routes_to_the_parser_with_is_credit_card_true_when_the_account_id_is_a_credit_card()
    {
        using var db = new SqliteInMemoryDatabase();
        var payer = RevisionSeed.NewAccount("Payer");
        var creditCard = RevisionSeed.NewCreditCard("Card", payer);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(payer);
            seed.CreditCards.Add(creditCard);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var parser = StubParser([], []);
        var handler = new ImportStatementCommandHandler(ctx, parser, NoOpResolver());

        await handler.Handle(new ImportStatementCommand(creditCard.Id, "Tipo;Status|"), CancellationToken.None);

        parser.Received(1).Parse("Tipo;Status|", true, creditCard.Id, out Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task Routes_to_the_parser_with_is_credit_card_false_when_the_account_id_is_a_checking_account()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount("Checking");

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var parser = StubParser([], []);
        var handler = new ImportStatementCommandHandler(ctx, parser, NoOpResolver());

        await handler.Handle(new ImportStatementCommand(account.Id, "Tipo;Status|"), CancellationToken.None);

        parser.Received(1).Parse("Tipo;Status|", false, account.Id, out Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task Result_dto_passes_through_lines_imported_and_warnings_from_the_parser_verbatim()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount("Checking");
        var category = RevisionSeed.NewCategory();

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var parsed = new List<Transaction> { StubParsedTransaction(account.Id, category.Id), StubParsedTransaction(account.Id, category.Id) };
        var warnings = new List<string> { "Linha 4: campo inválido." };
        var parser = StubParser(parsed, warnings);
        var handler = new ImportStatementCommandHandler(ctx, parser, NoOpResolver());

        var result = await handler.Handle(new ImportStatementCommand(account.Id, "content"), CancellationToken.None);

        result.LinesImported.Should().Be(2);
        result.Warnings.Should().Equal(warnings);
        result.LinesProcessed.Should().Be(3);
    }

    [Fact]
    public async Task Every_parsed_transaction_is_resolved_and_added_to_the_context()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount("Checking");
        var category = RevisionSeed.NewCategory();

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            await seed.SaveChangesAsync();
        }

        var resolver = NoOpResolver();
        await using (var ctx = db.NewContext())
        {
            var parsed = new List<Transaction> { StubParsedTransaction(account.Id, category.Id), StubParsedTransaction(account.Id, category.Id) };
            var parser = StubParser(parsed, []);
            var handler = new ImportStatementCommandHandler(ctx, parser, resolver);

            await handler.Handle(new ImportStatementCommand(account.Id, "content"), CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        await resolver.Received(2).ResolveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());

        await using var assert = db.NewContext();
        var count = await assert.Transactions.CountAsync(t => t.AccountId == account.Id);
        count.Should().Be(2);
    }

    [Fact]
    public async Task The_whole_batch_commits_atomically_via_transaction_behavior()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount("Checking");
        var category = RevisionSeed.NewCategory();

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            seed.TransactionCategories.Add(category);
            await seed.SaveChangesAsync();
        }

        await using (var ctx = db.NewContext())
        {
            var command = new ImportStatementCommand(account.Id, "content");
            var parsed = new List<Transaction> { StubParsedTransaction(account.Id, category.Id) };
            var parser = StubParser(parsed, []);
            var handler = new ImportStatementCommandHandler(ctx, parser, NoOpResolver());
            var behavior = new TransactionBehavior<ImportStatementCommand, ImportStatementResultDto>(
                ctx, Substitute.For<ILogger<TransactionBehavior<ImportStatementCommand, ImportStatementResultDto>>>());

            RequestHandlerDelegate<ImportStatementResultDto> next = async () =>
            {
                await handler.Handle(command, CancellationToken.None);
                throw new InvalidOperationException("boom after the handler mutated the context");
            };

            var act = async () => await behavior.Handle(command, next, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using var assert = db.NewContext();
        var count = await assert.Transactions.CountAsync(t => t.AccountId == account.Id);
        count.Should().Be(0, "the half-committed batch must roll back entirely");
    }
}
