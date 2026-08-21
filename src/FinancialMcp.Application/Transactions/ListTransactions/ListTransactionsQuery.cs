using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Enums;
using MediatR;

namespace FinancialMcp.Application.Transactions.ListTransactions;

/// <summary>
/// Lists checking account and/or credit card transactions with filters. Corresponds to the MCP tool
/// `list_transactions` (see CLAUDE.md > MCP). PeriodStart/PeriodEnd are required; all other filters
/// are optional.
/// </summary>
public sealed record ListTransactionsQuery(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    TransactionType? Type = null,
    TransactionStatus? Status = null,
    string? ParentCategory = null,
    string? Subcategory = null,
    Guid? AccountId = null,
    int? Year = null,
    int? Month = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<TransactionDto>>;

/// <summary>Paginated result — default pagination strategy for list_transactions on large datasets.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
