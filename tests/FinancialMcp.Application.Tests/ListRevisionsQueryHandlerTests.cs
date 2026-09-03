using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Revisions.ListRevisions;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Domain.Entities;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #15 — <c>list_revisions</c> returns pending <c>transaction_revisions</c> ordered by
/// <c>CreatedAt</c> ascending (oldest first), projected to the transaction DTO shape plus
/// <c>createdAt</c>.
/// </summary>
public class ListRevisionsQueryHandlerTests
{
    private static TransactionRevision Revision(string description, DateTimeOffset createdAt, decimal amount = -123.45m)
    {
        var account = RevisionSeed.NewAccount();
        var category = RevisionSeed.NewCategory();
        var parent = RevisionSeed.NewParentTransaction(account, category);

        var revision = RevisionSeed.NewRevision(parent, account, category, description, createdAt, amount);
        revision.TransactionId = parent.Id;
        return revision;
    }

    private static IApplicationDbContext DbWith(params TransactionRevision[] revisions)
    {
        // Build the mock DbSet before touching the substitute — BuildMockDbSet() configures
        // its own NSubstitute internally and would otherwise clobber the pending Returns() call
        // (same gotcha as CreateTransactionCommandValidatorTests).
        var set = revisions.ToList().BuildMockDbSet();
        var db = Substitute.For<IApplicationDbContext>();
        db.TransactionRevisions.Returns(set);
        return db;
    }

    [Fact]
    public async Task Orders_pending_revisions_oldest_first()
    {
        var db = DbWith(
            Revision("newest", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)),
            Revision("oldest", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            Revision("middle", new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)));

        var result = await new ListRevisionsQueryHandler(db).Handle(new ListRevisionsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Select(r => r.Description).Should().ContainInOrder("oldest", "middle", "newest");
        result.Items[0].CreatedAt.Should().BeBefore(result.Items[1].CreatedAt);
        result.Items[1].CreatedAt.Should().BeBefore(result.Items[2].CreatedAt);
    }

    [Fact]
    public async Task Projects_the_transaction_dto_shape_plus_created_at()
    {
        var createdAt = new DateTimeOffset(2026, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var db = DbWith(Revision("only one", createdAt, amount: -99.90m));

        var result = await new ListRevisionsQueryHandler(db).Handle(new ListRevisionsQuery(), CancellationToken.None);

        var dto = result.Items.Should().ContainSingle().Subject;
        dto.Description.Should().Be("only one");
        dto.Amount.Should().Be(-99.90m);
        dto.RawCategory.Should().Be("Moradia");
        dto.Status.Should().Be("Revision");
        dto.CreatedAt.Should().Be(createdAt);
        dto.TransactionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Paginates_while_keeping_the_oldest_first_ordering()
    {
        var db = DbWith(
            Revision("r1", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            Revision("r2", new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)),
            Revision("r3", new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero)));

        var page2 = await new ListRevisionsQueryHandler(db)
            .Handle(new ListRevisionsQuery(Page: 2, PageSize: 2), CancellationToken.None);

        page2.TotalCount.Should().Be(3);
        page2.Items.Select(r => r.Description).Should().Equal("r3");
    }
}
