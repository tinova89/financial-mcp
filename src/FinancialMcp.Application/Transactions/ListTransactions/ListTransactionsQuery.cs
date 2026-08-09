using FinancialMcp.Application.Transactions.CreateTransaction;
using MediatR;

namespace FinancialMcp.Application.Transactions.ListTransactions;

/// <summary>
/// Lists checking account and/or credit card transactions with filters. Corresponds to the MCP tool
/// `list_transactions` (see CLAUDE.md > MCP). All filters are optional.
/// </summary>
public sealed record ListTransactionsQuery(
    string? Source = null,
    string? Type = null,
    string? Status = null,
    string? ParentCategory = null,
    string? Subcategory = null,
    Guid? AccountId = null,
    Guid? CardId = null,
    DateOnly? PeriodStart = null,
    DateOnly? PeriodEnd = null,
    int? Year = null,
    int? Month = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<TransactionDto>>;

/// <summary>Paginated result — default pagination strategy for list_transactions on large datasets.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
