using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Revisions.ApproveRevision;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #15 — approving a revision <b>moves</b> (never copies) the row: a new
/// <c>Scheduled</c> transaction appears, the <c>transaction_revisions</c> row is gone,
/// <c>SubmittedForReviewAt</c> is the revision's <c>CreatedAt</c> verbatim, and the
/// insert + delete are atomic.
/// </summary>
public class ApproveRevisionCommandHandlerTests
{
    private static readonly DateTimeOffset RevisionCreatedAt =
        new(2026, 5, 4, 3, 2, 1, TimeSpan.Zero);

    [Fact]
    public async Task Moves_the_revision_onto_a_new_scheduled_transaction_and_deletes_it()
    {
        using var db = new SqliteInMemoryDatabase();
        Guid revisionId;

        await using (var seed = db.NewContext())
        {
            var (account, category, parent) = await RevisionSeed.SeedGraphAsync(seed);
            var revision = RevisionSeed.NewRevision(parent, account, category, "approve me", RevisionCreatedAt);
            seed.TransactionRevisions.Add(revision);
            await seed.SaveChangesAsync();
            revisionId = revision.Id;
        }

        TransactionDto dto;
        await using (var act = db.NewContext())
        {
            var handler = new ApproveRevisionCommandHandler(act);
            dto = await handler.Handle(new ApproveRevisionCommand(revisionId, Confirm: true), CancellationToken.None);

            // In production TransactionBehavior owns the final SaveChanges + commit.
            await act.SaveChangesAsync();
        }

        dto.Status.Should().Be(nameof(TransactionStatus.Scheduled));
        dto.Id.Should().NotBe(revisionId, "the approval creates a brand-new transactions row");

        await using var assert = db.NewContext();

        assert.TransactionRevisions.Should().BeEmpty("the revision row was moved, not copied");

        var created = await assert.Transactions.SingleAsync(t => t.Description == "approve me");
        created.Status.Should().Be(TransactionStatus.Scheduled);
        created.ScheduledAt.Should().NotBeNull("entering Scheduled stamps ScheduledAt");
        created.SubmittedForReviewAt.Should().Be(RevisionCreatedAt,
            "it must be the revision's CreatedAt copied verbatim, never regenerated");
        created.Amount.Should().Be(-123.45m);
    }

    [Fact]
    public async Task Returns_404_when_the_revision_does_not_exist()
    {
        using var db = new SqliteInMemoryDatabase();
        await using var ctx = db.NewContext();
        var handler = new ApproveRevisionCommandHandler(ctx);

        var act = async () => await handler.Handle(
            new ApproveRevisionCommand(Guid.NewGuid(), Confirm: true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Insert_and_delete_roll_back_together_when_the_unit_fails()
    {
        using var db = new SqliteInMemoryDatabase();
        Guid revisionId;

        await using (var seed = db.NewContext())
        {
            var (account, category, parent) = await RevisionSeed.SeedGraphAsync(seed);
            var revision = RevisionSeed.NewRevision(parent, account, category, "approve me", RevisionCreatedAt);
            seed.TransactionRevisions.Add(revision);
            await seed.SaveChangesAsync();
            revisionId = revision.Id;
        }

        await using (var ctx = db.NewContext())
        {
            var command = new ApproveRevisionCommand(revisionId, Confirm: true);
            var handler = new ApproveRevisionCommandHandler(ctx);
            var behavior = new TransactionBehavior<ApproveRevisionCommand, TransactionDto>(
                ctx,
                Substitute.For<ILogger<TransactionBehavior<ApproveRevisionCommand, TransactionDto>>>());

            RequestHandlerDelegate<TransactionDto> next = async () =>
            {
                await handler.Handle(command, CancellationToken.None);
                throw new InvalidOperationException("boom after the handler mutated the context");
            };

            var act = async () => await behavior.Handle(command, next, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using var assert = db.NewContext();
        assert.TransactionRevisions.Should().ContainSingle(r => r.Id == revisionId,
            "a failed approval must leave the revision untouched");
        assert.Transactions.Should().NotContain(t => t.Description == "approve me",
            "the half-done insert must roll back with the delete");
    }
}
