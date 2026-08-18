namespace FinancialMcp.Application.BudgetGoals.CreateCategoryBudget;

/// <summary>Response DTO — never expose the BudgetGoal domain entity directly (see CLAUDE.md > DTOs).</summary>
public sealed record BudgetGoalDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string CurrencyCode,
    string Period,
    int Year,
    int Month);
