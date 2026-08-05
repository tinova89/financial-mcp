namespace FinancialMcp.Domain.ValueObjects;

/// <summary>
/// Value object para "Categoria-mãe/Subcategoria". Centraliza o parsing usado por
/// todas as ferramentas MCP que agregam por categoria (ver CLAUDE.md > Convenções
/// de Código > Categoria/Subcategoria). Único ponto de verdade para o split por "/".
/// </summary>
public sealed record Categoria
{
    private const char Separador = '/';

    public string CategoriaMae { get; }

    /// <summary>Nulo quando a categoria original não tinha subcategoria.</summary>
    public string? Subcategoria { get; }

    /// <summary>Valor original completo, exatamente como veio do extrato.</summary>
    public string ValorCompleto { get; }

    private Categoria(string categoriaMae, string? subcategoria, string valorCompleto)
    {
        CategoriaMae = categoriaMae;
        Subcategoria = subcategoria;
        ValorCompleto = valorCompleto;
    }

    public static Categoria Parse(string valorBruto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valorBruto);

        var partes = valorBruto.Split(Separador, 2, StringSplitOptions.TrimEntries);

        return partes.Length == 2
            ? new Categoria(partes[0], partes[1], valorBruto)
            : new Categoria(partes[0], null, valorBruto);
    }

    /// <summary>True quando a meta foi cadastrada com subcategoria completa (match exato).</summary>
    public bool TemSubcategoria => Subcategoria is not null;

    /// <summary>
    /// Compara esta categoria (de uma transação) contra a categoria de uma meta.
    /// Meta só com categoria-mãe (ex.: "Moradia") casa com qualquer subcategoria;
    /// meta com subcategoria completa (ex.: "Moradia/Seguro") exige match exato.
    /// </summary>
    public bool CasaComMeta(Categoria categoriaDaMeta)
    {
        if (!string.Equals(CategoriaMae, categoriaDaMeta.CategoriaMae, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !categoriaDaMeta.TemSubcategoria
            || string.Equals(Subcategoria, categoriaDaMeta.Subcategoria, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => ValorCompleto;
}
