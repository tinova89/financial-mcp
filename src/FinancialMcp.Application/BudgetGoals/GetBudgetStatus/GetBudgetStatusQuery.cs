using MediatR;

namespace FinancialMcp.Application.BudgetGoals.GetBudgetStatus;

/// <summary>
/// Corresponde à tool MCP `get_budget_status`. Calcula Gasto_Real, Saldo_Meta e
/// % Utilizado por categoria/mês, conforme CLAUDE.md > Regras de Negócio > Metas
/// de orçamento. Ano/Mês são obrigatórios; sem eles a consulta seria ambígua.
/// </summary>
public sealed record GetBudgetStatusQuery(int Ano, int Mes) : IRequest<IReadOnlyList<BudgetStatusDto>>;

public sealed record BudgetStatusDto(
    string CategoriaBruta,
    decimal MetaValor,
    decimal GastoReal,
    decimal SaldoMeta,
    decimal? PercentualUtilizado,
    IReadOnlyList<SubcategoriaBreakdownDto> DetalhamentoPorSubcategoria);

/// <summary>Detalhamento secundário por subcategoria — não cria meta própria (ver CLAUDE.md).</summary>
public sealed record SubcategoriaBreakdownDto(string? Subcategoria, decimal GastoReal);
