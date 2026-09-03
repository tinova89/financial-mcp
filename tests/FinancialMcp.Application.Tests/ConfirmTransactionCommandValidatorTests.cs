using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Application.Transactions.ConfirmTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #16 — <c>confirm_transaction</c> only promotes a transaction that is currently
/// <see cref="TransactionStatus.Scheduled"/>. A <c>Revision</c> row or an
/// already-<c>Confirmed</c> row is rejected by validation before the handler runs; an
/// unknown id is left for the handler's canonical 404.
/// </summary>
public class ConfirmTransactionCommandValidatorTests
{
    private static Transaction NewTransaction(Account account, TransactionCategory category, TransactionStatus status) => new()
    {
        Type = TransactionType.Expense,
        Status = status,
        Description = $"tx {status}",
        Amount = -10m,
        Account = account,
        Category = category,
        ExpectedDate = new DateOnly(2026, 3, 1),
        Recurrence = RecurrenceType.None,
    };

    private static async Task<(SqliteInMemoryDatabase Db, Guid Id)> SeedTransactionAsync(TransactionStatus status)
    {
        var db = new SqliteInMemoryDatabase();

        await using var seed = db.NewContext();
        var (account, category, _) = await RevisionSeed.SeedGraphAsync(seed);
        var tx = NewTransaction(account, category, status);
        seed.Transactions.Add(tx);
        await seed.SaveChangesAsync();

        return (db, tx.Id);
    }

    [Theory]
    [InlineData(TransactionStatus.Revision)]
    [InlineData(TransactionStatus.Confirmed)]
    public async Task Rejects_confirming_a_transaction_that_is_not_scheduled(TransactionStatus status)
    {
        var (db, id) = await SeedTransactionAsync(status);
        using var _ = db;

        await using var ctx = db.NewContext();
        var validator = new ConfirmTransactionCommandValidator(ctx);

        var result = await validator.TestValidateAsync(new ConfirmTransactionCommand(id));

        result.ShouldHaveValidationErrorFor(x => x.TransactionId);
    }

    [Fact]
    public async Task Accepts_confirming_a_scheduled_transaction()
    {
        var (db, id) = await SeedTransactionAsync(TransactionStatus.Scheduled);
        using var _ = db;

        await using var ctx = db.NewContext();
        var validator = new ConfirmTransactionCommandValidator(ctx);

        var result = await validator.TestValidateAsync(new ConfirmTransactionCommand(id));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Leaves_an_unknown_id_for_the_handler_to_404()
    {
        using var db = new SqliteInMemoryDatabase();

        await using var ctx = db.NewContext();
        var validator = new ConfirmTransactionCommandValidator(ctx);

        var result = await validator.TestValidateAsync(new ConfirmTransactionCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
