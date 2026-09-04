using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.ListTransactions;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Revisions.ListRevisions;

/// <summary>
/// Single handler for <see cref="ListRevisionsQuery"/>. Read-only projection of
/// <c>transaction_revisions</c> to <see cref="RevisionDto"/>, ordered by <c>CreatedAt</c>
/// ascending so the oldest pending revision comes first.
/// </summary>
public sealed class ListRevisionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListRevisionsQuery, PagedResult<RevisionDto>>
{
    public async Task<PagedResult<RevisionDto>> Handle(ListRevisionsQuery request, CancellationToken cancellationToken)
    {
        var query = db.TransactionRevisions.AsNoTracking()
            .Include(r => r.Category).ThenInclude(c => c.ParentCategory)
            .AsQueryable();

        if (request.AccountId is not null)
        {
            query = query.Where(r => r.AccountId == request.AccountId);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            // Same parent-or-subcategory match as list_transactions: a filter of "Moradia"
            // matches revisions categorized as "Moradia" or as any subcategory under it.
            query = query.Where(r =>
                (r.Category.ParentCategoryId == null && r.Category.Name == request.Category) ||
                (r.Category.ParentCategory != null && r.Category.ParentCategory.Name == request.Category));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var revisions = await query
            .OrderBy(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = revisions.Select(Map).ToList();

        return new PagedResult<RevisionDto>(items, request.Page, request.PageSize, totalCount);
    }

    private static RevisionDto Map(TransactionRevision r) => new(
        r.Id,
        r.TransactionId,
        r.Type.ToString(),
        r.Status.ToString(),
        r.Description,
        r.Amount,
        r.Category.FullName,
        r.ExpectedDate,
        r.ActualDate,
        r.ConfirmationDate,
        r.InvoiceDueDate,
        r.Recurrence.ToString(),
        r.CurrentInstallment,
        r.TotalInstallments,
        r.AccountId,
        r.CreatedAt);
}
