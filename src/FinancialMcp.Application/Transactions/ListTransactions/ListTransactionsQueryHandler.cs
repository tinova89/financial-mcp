using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.ListTransactions;

/// <summary>
/// Single handler for ListTransactionsQuery. Pure category calculation rule
/// (parsing "Categoria-mãe/Subcategoria") delegated to the domain's Category value object.
/// </summary>
public sealed class ListTransactionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListTransactionsQuery, PagedResult<TransactionDto>>
{
    public async Task<PagedResult<TransactionDto>> Handle(ListTransactionsQuery request, CancellationToken cancellationToken)
    {
        // Include(Account) is required for GetReferenceMonthYear() below, which reads
        // Account.Kind to pick the right reference-date column.
        var query = db.Transactions.AsNoTracking().Include(t => t.Account).AsQueryable();

        if (request.Type is not null)
        {
            query = query.Where(t => t.Type == request.Type);
        }

        if (request.Status is not null)
        {
            query = query.Where(t => t.Status == request.Status);
        }

        if (request.AccountId is not null)
        {
            query = query.Where(t => t.AccountId == request.AccountId);
        }

        if (request.PeriodStart is not null)
        {
            query = query.Where(t => t.ExpectedDate >= request.PeriodStart);
        }

        if (request.PeriodEnd is not null)
        {
            query = query.Where(t => t.ExpectedDate <= request.PeriodEnd);
        }

        if (request.ParentCategory is not null)
        {
            // EF.Functions.Like avoids bringing everything into memory before filtering by parent category.
            query = query.Where(t => EF.Functions.Like(t.RawCategory, $"{request.ParentCategory}%"));
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
            .Where(t => request.Year is null || t.GetReferenceMonthYear()?.Year == request.Year)
            .Where(t => request.Month is null || t.GetReferenceMonthYear()?.Month == request.Month)
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
        t.RawCategory,
        t.ExpectedDate,
        t.ActualDate,
        t.ReconciledDate,
        t.InvoiceDueDate,
        t.Recurrence.ToString(),
        t.CurrentInstallment,
        t.TotalInstallments,
        t.AccountId);
}
