using System.ComponentModel;
using FinancialMcp.Api.Auth;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Revisions.ApproveRevision;
using FinancialMcp.Application.Revisions.ListRevisions;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Application.Transactions.ListTransactions;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

/// <summary>
/// MCP tools for <c>transaction_revisions</c> — the proposed field values submitted while a
/// transaction sits in <c>Status = Revision</c> (Card #14), reviewed/approved by Card #15.
/// Each tool is "thin": it only builds the MediatR request and calls <c>IMediator.Send</c>
/// (see CLAUDE.md &gt; Mediator Pattern).
///
/// <para>
/// <c>approve_revision</c> additionally requires the <c>approval</c> JWT scope. Because MCP
/// tools are reflection-registered methods with no ASP.NET Core endpoint metadata, the scope
/// is checked here, inline, before the command is dispatched — a missing scope throws
/// <see cref="ForbiddenException"/> (mapped to <c>403</c>) and MediatR is never reached. The
/// equivalent endpoint policy is <see cref="AuthScopes.ApprovalPolicy"/>.
/// </para>
/// </summary>
[McpServerToolType]
public sealed class RevisionTools(IMediator mediator, ICurrentUserService currentUser)
{
    [McpServerTool(Name = "list_revisions"), Description(
        """
        Lists every pending transaction revision (a proposed set of field values submitted
        while a transaction is in `Revision` status, awaiting approval), paginated.

        ## Parameters
        - **page** — 1-based page number. Optional, defaults to `1`.
        - **pageSize** — Items per page. Optional, defaults to `50`.
        - **category** — Filter to revisions whose category starts with this parent (e.g.
          `"Moradia"` matches both `Moradia` and `Moradia/Seguro`). Optional.
        - **accountId** — `Guid` matching a checking account or a credit card's own id.
          Optional.

        ## Behavior
        - Read-only; requires only the normal read scope (no `approval` scope).
        - Ordered by `createdAt` **ascending** — the oldest pending revision first.
        - All provided filters combine with AND semantics.

        ## Example
        ```json
        { "page": 1, "pageSize": 20, "category": "Moradia" }
        ```

        ## Returns
        A `PagedResult<RevisionDto>` (items, page, pageSize, totalCount, totalPages). Each
        item has the same shape as a `TransactionDto` (id, type, status, description, amount,
        rawCategory, expectedDate, actualDate, confirmedDate, invoiceDueDate, recurrence,
        currentInstallment, totalInstallments, accountId) plus `transactionId` (the parent
        transaction) and `createdAt` (the Revision-stage submission timestamp — the value
        copied verbatim into the transaction's `submittedForReviewAt` on approval).
        """)]
    public Task<PagedResult<RevisionDto>> ListRevisionsAsync(
        int page = 1, int pageSize = 50,
        string? category = null, Guid? accountId = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ListRevisionsQuery(page, pageSize, category, accountId), cancellationToken);

    [McpServerTool(Name = "approve_revision"), Description(
        """
        Approves a pending transaction revision: its field values are **moved** onto a new
        `Scheduled` transaction and the revision row is deleted. Not a copy — after approval
        the revision no longer exists.

        ## Parameters
        - **revisionId** — `Guid` of the `transaction_revisions` row to approve. Required.
        - **confirm** — Must be explicitly `true` for the operation to proceed. Required
          (same guard as `delete_transaction`); `false`/omitted is rejected by validation
          before any data is touched.

        ## Authorization
        Requires the **`approval`** JWT scope. A caller without it gets `403 Forbidden`
        before the request is dispatched — this is not a validation error and cannot be
        retried by changing arguments.

        ## Behavior
        - Throws a not-found error if `revisionId` doesn't match a pending revision.
        - The new transaction gets `status = Scheduled`, a fresh `scheduledAt` stamp, and
          `submittedForReviewAt` copied **verbatim** from the revision's `createdAt` (never
          regenerated).
        - The insert (new transaction) and the delete (revision row) commit atomically.

        ## Example
        ```json
        { "revisionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "confirm": true }
        ```

        ## Returns
        The created `TransactionDto` (status `Scheduled`), with `remainingBudget`/
        `remainingBudgetPercentage` left `null`.
        """)]
    public Task<TransactionDto> ApproveRevisionAsync(
        Guid revisionId, bool confirm, CancellationToken cancellationToken = default)
    {
        // Scope check happens here, before MediatR — a missing `approval` scope is a 403,
        // never a validation failure (see the class remarks).
        if (!currentUser.HasScope(AuthScopes.Approval))
        {
            throw new ForbiddenException(AuthScopes.Approval);
        }

        return mediator.Send(new ApproveRevisionCommand(revisionId, confirm), cancellationToken);
    }
}
