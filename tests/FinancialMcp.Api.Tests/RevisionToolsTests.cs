using FinancialMcp.Api.Auth;
using FinancialMcp.Api.Mcp.Tools;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Revisions.ApproveRevision;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Enums;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Api.Tests;

/// <summary>
/// Card #15 — the <c>approve_revision</c> MCP tool enforces the <c>approval</c> JWT scope
/// itself, before dispatching: a missing scope is a <c>403</c> (<see cref="ForbiddenException"/>),
/// not a validation error, and MediatR is never reached.
/// </summary>
public class RevisionToolsTests
{
    private static TransactionDto AnyTransactionDto() => new(
        Guid.NewGuid(), nameof(TransactionType.Expense), nameof(TransactionStatus.Scheduled),
        "x", -1m, "Moradia", new DateOnly(2026, 2, 1), null, null, null,
        nameof(RecurrenceType.None), null, null, Guid.NewGuid(), NeedsConfirmation: false);

    [Fact]
    public async Task approve_revision_is_403_when_the_approval_scope_is_missing()
    {
        var mediator = Substitute.For<IMediator>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.HasScope(AuthScopes.Approval).Returns(false);

        var tools = new RevisionTools(mediator, currentUser);

        var act = async () => await tools.ApproveRevisionAsync(Guid.NewGuid(), confirm: true);

        (await act.Should().ThrowAsync<ForbiddenException>())
            .Which.RequiredScope.Should().Be(AuthScopes.Approval);

        await mediator.DidNotReceive().Send(
            Arg.Any<ApproveRevisionCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task approve_revision_dispatches_the_command_when_the_approval_scope_is_present()
    {
        var mediator = Substitute.For<IMediator>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.HasScope(AuthScopes.Approval).Returns(true);
        mediator.Send(Arg.Any<ApproveRevisionCommand>(), Arg.Any<CancellationToken>())
            .Returns(AnyTransactionDto());

        var tools = new RevisionTools(mediator, currentUser);
        var revisionId = Guid.NewGuid();

        await tools.ApproveRevisionAsync(revisionId, confirm: true);

        await mediator.Received(1).Send(
            Arg.Is<ApproveRevisionCommand>(c => c.RevisionId == revisionId && c.Confirm),
            Arg.Any<CancellationToken>());
    }
}
