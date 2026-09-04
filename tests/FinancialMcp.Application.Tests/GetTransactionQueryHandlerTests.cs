using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Application.Transactions.GetTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `get_transaction` returns the full DTO for a known id, 404s for an unknown
/// id, and excludes a soft-deleted transaction via the global query filter — which requires
/// a real <see cref="SqliteInMemoryDatabase"/>, since a mocked DbSet never applies it.
/// </summary>
public class GetTransactionQueryHandlerTests
{
    private static readonly DateOnly FutureExpectedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(5);

    private static Transaction NewTransaction(Account account, TransactionCategory category) => new()
    {
        Type = TransactionType.Expense,
        Status = TransactionStatus.Scheduled,
        Description = "Compra no mercado",
        Amount = -42.50m,
        Account = account,
        Category = category,
        ExpectedDate = FutureExpectedDate,
        Recurrence = RecurrenceType.None,
    };

    [Fact]
    public async Task Returns_the_full_dto_for_a_known_transaction()
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
        var dto = await new GetTransactionQueryHandler(ctx)
            .Handle(new GetTransactionQuery(transactionId), CancellationToken.None);

        dto.Id.Should().Be(transactionId);
        dto.Type.Should().Be(nameof(TransactionType.Expense));
        dto.Status.Should().Be(nameof(TransactionStatus.Scheduled));
        dto.Description.Should().Be("Compra no mercado");
        dto.Amount.Should().Be(-42.50m);
        dto.RawCategory.Should().Be("Moradia", "the DTO's RawCategory is mapped from Category.FullName");
        dto.ExpectedDate.Should().Be(FutureExpectedDate);
        dto.NeedsConfirmation.Should().BeFalse("ExpectedDate is in the future");
    }

    [Fact]
    public async Task Throws_not_found_for_an_unknown_id()
    {
        using var db = new SqliteInMemoryDatabase();
        await using var ctx = db.NewContext();

        var act = async () => await new GetTransactionQueryHandler(ctx)
            .Handle(new GetTransactionQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Excludes_a_soft_deleted_transaction()
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

        await using (var deleteCtx = db.NewContext())
        {
            var tx = await deleteCtx.Transactions.SingleAsync(t => t.Id == transactionId);
            tx.MarkAsDeleted();
            await deleteCtx.SaveChangesAsync();
        }

        await using var assertCtx = db.NewContext();
        var act = async () => await new GetTransactionQueryHandler(assertCtx)
            .Handle(new GetTransactionQuery(transactionId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("the global soft-delete query filter excludes it");
    }
}
