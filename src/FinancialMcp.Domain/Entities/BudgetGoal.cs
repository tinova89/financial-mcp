using FinancialMcp.Domain.Common;
using FinancialMcp.Domain.ValueObjects;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// Budget goal per category/month (Meta_Valor), sourced from metas_orcamento.csv.
/// Can be registered with only the parent category (e.g. "Moradia") or with the full
/// subcategory (e.g. "Moradia/Seguro") — see CLAUDE.md > Category and subcategory.
/// </summary>
public class BudgetGoal : BaseEntity
{
    public string RawCategory { get; set; } = default!;
    public Category Category => Category.Parse(RawCategory);

    public int Year { get; set; }
    public int Month { get; set; }
    public MonthYear MonthYear => new(Year, Month);

    public decimal BudgetAmount { get; set; }
}
