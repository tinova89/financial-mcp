namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Coluna "Repetição" do extrato de cartão. Ver CLAUDE.md > Regras de Negócio >
/// Cartão de crédito — ciclo de fatura, parcelamento e projeção.
/// </summary>
public enum TipoRepeticao
{
    Nenhuma,
    Parcelado,
    FixoMes
}
