using FinancialMcp.Application.Transactions.ListTransactions;
using MediatR;

namespace FinancialMcp.Application.Revisions.ListRevisions;

/// <summary>
/// Lists every pending <c>transaction_revisions</c> row (proposed field values awaiting
/// approval — Card #15), paginated and ordered by <c>CreatedAt</c> ascending (oldest first).
/// Corresponds to the MCP tool <c>list_revisions</c> (see CLAUDE.md &gt; MCP). Read-only;
/// needs only the normal read scope, no <c>approval</c> scope.
/// </summary>
public sealed record ListRevisionsQuery(
    int Page = 1,
    int PageSize = 50,
    string? Category = null,
    Guid? AccountId = null) : IRequest<PagedResult<RevisionDto>>;
