using FinancialMcp.Application.Common.Behaviors;
using FinancialMcp.Domain.Enums;
using MediatR;

namespace FinancialMcp.Application.BudgetGoals.CreateCategoryBudget;

/// <summary>
/// Registers a budget goal for a parent TransactionCategory (never a subcategory) for a
/// given calendar month, per CLAUDE.md > Business Rules > Budget goals. Corresponds to the
/// MCP tool `create_category_budget`.
/// </summary>
public sealed record CreateCategoryBudgetCommand(
    Guid CategoryId,
    decimal Amount,
    string CurrencyCode,
    BudgetPeriodType Period,
    int Year,
    int Month
) : IRequest<BudgetGoalDto>, ITransactionalRequest;
