namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Status da transação. CC usa Conciliado/Agendado; CD usa Conciliado/Nconciliado.
/// Ver CLAUDE.md > Regras de Negócio > Metas de orçamento (filtro de status).
/// </summary>
public enum StatusTransacao
{
    Conciliado,
    Agendado,     // apenas Conta Corrente
    Nconciliado   // apenas Cartão de Crédito — previsto, sujeito a alteração até o fechamento
}
