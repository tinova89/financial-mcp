using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.BudgetGoals.CreateCategoryBudget;

/// <summary>
/// Single handler for CreateCategoryBudgetCommand — orchestrates persistence of the new
/// budget goal. CategoryId's existence and its parent-only constraint (never a subcategory)
/// are enforced by CreateCategoryBudgetCommandValidator before this runs.
/// </summary>
public sealed class CreateCategoryBudgetCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateCategoryBudgetCommand, BudgetGoalDto>
{
    public async Task<BudgetGoalDto> Handle(CreateCategoryBudgetCommand request, CancellationToken cancellationToken)
    {
        var category = await db.TransactionCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(nameof(TransactionCategory), request.CategoryId);
        }

        var budgetGoal = new BudgetGoal
        {
            RawCategoryId = category.Id,
            BudgetAmount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            Period = request.Period,
            Year = request.Year,
            Month = request.Month,
        };

        db.BudgetGoals.Add(budgetGoal);

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new BudgetGoalDto(
            budgetGoal.Id, category.Id, category.Name, budgetGoal.BudgetAmount,
            budgetGoal.CurrencyCode, budgetGoal.Period.ToString(), budgetGoal.Year, budgetGoal.Month);
    }
}
