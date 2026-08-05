namespace FinancialMcp.Domain.Enums;

/// <summary>Coluna "Tipo" do extrato. Ver CLAUDE.md > Regras de Negócio > Metas de orçamento.</summary>
public enum TipoTransacao
{
    Despesa,
    Receita,
    Transferencia,
    Pagamento // "Pagamento de cartão" — nunca entra no cálculo de metas (evita duplicidade).
}
