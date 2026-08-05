using FinancialMcp.Domain.Common;
using FinancialMcp.Domain.ValueObjects;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// Meta de orçamento por categoria/mês (Meta_Valor), origem de metas_orcamento.csv.
/// Pode ser cadastrada apenas com categoria-mãe (ex.: "Moradia") ou com subcategoria
/// completa (ex.: "Moradia/Seguro") — ver CLAUDE.md > Categoria e subcategoria.
/// </summary>
public class MetaOrcamento : BaseEntity
{
    public string CategoriaBruta { get; set; } = default!;
    public Categoria Categoria => Categoria.Parse(CategoriaBruta);

    public int Ano { get; set; }
    public int Mes { get; set; }
    public MesAno MesAno => new(Ano, Mes);

    public decimal MetaValor { get; set; }
}
