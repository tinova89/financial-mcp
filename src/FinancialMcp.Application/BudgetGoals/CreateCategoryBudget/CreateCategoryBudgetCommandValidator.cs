using FinancialMcp.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.BudgetGoals.CreateCategoryBudget;

public sealed class CreateCategoryBudgetCommandValidator : AbstractValidator<CreateCategoryBudgetCommand>
{
    public CreateCategoryBudgetCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Amount).GreaterThan(0m);

        RuleFor(x => x.CurrencyCode).NotEmpty()
            .Must(code => FinancialCurrency.All.Any(c => c.CurrencyCode == code))
            .WithMessage(x => $"CurrencyCode \"{x.CurrencyCode}\" não é reconhecido. Moedas suportadas: " +
                $"{string.Join(", ", FinancialCurrency.All.Select(c => c.CurrencyCode))}.");

        RuleFor(x => x.Period).IsInEnum();

        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);

        // Domain rule: a goal always targets a parent category, never a subcategory
        // (see BudgetGoal doc comment / CLAUDE.md > Category and subcategory).
        RuleFor(x => x.CategoryId)
            .MustAsync(async (categoryId, ct) =>
                !await db.TransactionCategories.AnyAsync(c => c.Id == categoryId && c.ParentCategoryId != null, ct))
            .WithMessage("CategoryId referencia uma subcategoria; um orçamento só pode ser criado para uma categoria-mãe.");

        // One goal per category/month (see BudgetGoalConfiguration's unique index) — checked
        // here so the caller gets a clear validation error instead of a raw DB constraint one.
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
                !await db.BudgetGoals.AnyAsync(g => g.RawCategoryId == cmd.CategoryId && g.Year == cmd.Year && g.Month == cmd.Month, ct))
            .WithMessage("Já existe um orçamento registrado para esta categoria neste ano/mês.")
            .WithName("CategoryId");
    }
}
