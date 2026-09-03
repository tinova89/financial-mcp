using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Application.Transactions.CreateTransaction;
using MediatR;

namespace FinancialMcp.Application.Revisions.ApproveRevision;

/// <summary>
/// Approves a pending <c>transaction_revisions</c> row (Card #15): the row is <b>moved</b>
/// (not copied) onto a brand-new <c>transactions</c> row with <c>Status = Scheduled</c>,
/// then deleted from <c>transaction_revisions</c>. The insert and the delete commit
/// atomically as a single unit via <see cref="ITransactionalRequest"/>
/// (<c>TransactionBehavior</c>).
///
/// <para>
/// <see cref="Confirm"/> must be explicitly <c>true</c> — same destructive-action guard as
/// <c>delete_transaction</c>, enforced by <see cref="ApproveRevisionCommandValidator"/>.
/// The <c>approval</c> JWT scope is enforced earlier, at the MCP tool level, before this
/// request is dispatched (see CLAUDE.md &gt; Authentication (Custom JWT) &gt; Scopes).
/// </para>
///
/// Returns the <see cref="TransactionDto"/> of the newly created (Scheduled) transaction.
/// </summary>
public sealed record ApproveRevisionCommand(Guid RevisionId, bool Confirm)
    : IRequest<TransactionDto>, ITransactionalRequest;
