using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.ListTransactions;

/// <summary>
/// Single handler for ListTransactionsQuery. Category filtering/output goes through the
/// persisted Category (TransactionCategory) relation — RawCategory is transient support
/// data only (see Transaction.RawCategory doc comment).
/// </summary>
public sealed class ListTransactionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListTransactionsQuery, PagedResult<TransactionDto>>
{
    public async Task<PagedResult<TransactionDto>> Handle(ListTransactionsQuery request, CancellationToken cancellationToken)
    {
        // Include(Account) is required for GetReferenceMonthYear() below, which reads
        // Account.Kind to pick the right reference-date column.
        var query = db.Transactions.AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category).ThenInclude(c => c.ParentCategory)
            .AsQueryable();

        if (request.Type is not null)
        {
            var type = Enum.Parse<TransactionType>(request.Type, ignoreCase: true);
            query = query.Where(t => t.Type == type);
        }

        if (request.Status is not null)
        {
            var status = Enum.Parse<TransactionStatus>(request.Status, ignoreCase: true);
            query = query.Where(t => t.Status == status);
        }

        if (request.AccountId is not null)
        {
            query = query.Where(t => t.AccountId == request.AccountId);
        }

        query = query.Where(t => t.ExpectedDate >= request.PeriodStart && t.ExpectedDate <= request.PeriodEnd);

        if (request.ParentCategory is not null)
        {
            // Filters via the Category relation (RawCategory isn't persisted — see
            // TransactionConfiguration.Ignore) — matches whether the transaction's category
            // *is* the parent (no subcategory) or is a subcategory under it.
            query = query.Where(t =>
                (t.Category.ParentCategoryId == null && t.Category.Name == request.ParentCategory) ||
                (t.Category.ParentCategory != null && t.Category.ParentCategory.Name == request.ParentCategory));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .OrderByDescending(t => t.ExpectedDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Subcategory/Year/Month filters depend on the Category value object and the
        // reference MonthYear calculated in memory (Data Conciliado vs. Venc. Fatura).
        var filteredItems = transactions
            .Where(t => request.Subcategory is null || t.Category.Subcategory == request.Subcategory)
            .Where(t => (request.Year is null || request.Year <= 0) || t.GetReferenceMonthYear()?.Year == request.Year)
            .Where(t => (request.Month is null || request.Month <= 0) || t.GetReferenceMonthYear()?.Month == request.Month)
            .Select(Map)
            .ToList();

        return new PagedResult<TransactionDto>(filteredItems, request.Page, request.PageSize, totalCount);
    }

    private static TransactionDto Map(Transaction t) => new(
        t.Id,
        t.Type.ToString(),
        t.Status.ToString(),
        t.Description,
        t.Amount,
        t.Category.FullName,
        t.ExpectedDate,
        t.ActualDate,
        t.ReconciledDate,
        t.InvoiceDueDate,
        t.Recurrence.ToString(),
        t.CurrentInstallment,
        t.TotalInstallments,
        t.AccountId);
}
