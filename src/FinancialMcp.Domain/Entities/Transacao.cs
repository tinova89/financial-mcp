using FinancialMcp.Domain.Common;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;

namespace FinancialMcp.Domain.Entities;

/// <summary>
/// Uma linha de extrato de Conta Corrente ou Cartão de Crédito. Ver CLAUDE.md >
/// Regras de Negócio do Domínio Financeiro para as regras de agregação/projeção
/// que consomem esta entidade.
/// </summary>
public class Transacao : BaseEntity
{
    public OrigemTransacao Origem { get; set; }
    public TipoTransacao Tipo { get; set; }
    public StatusTransacao Status { get; set; }

    public string Descricao { get; set; } = default!;

    /// <summary>Sempre decimal — nunca double/float (ver CLAUDE.md > Convenções de Código > Dinheiro).</summary>
    public decimal Valor { get; set; }

    public string CategoriaBruta { get; set; } = default!; // "Categoria-mãe/Subcategoria" como veio do extrato
    public Categoria Categoria => Categoria.Parse(CategoriaBruta);

    // Datas explícitas (nunca string) — ver CLAUDE.md > Convenções de Código > Datas.
    public DateOnly DataPrevista { get; set; }
    public DateOnly? DataEfetiva { get; set; }
    public DateOnly? DataConciliado { get; set; }   // apenas Conta Corrente, quando Status = Conciliado
    public DateOnly? VencimentoFatura { get; set; } // apenas Cartão de Crédito — data que impacta o saldo

    // Parcelamento / recorrência (apenas Cartão de Crédito).
    public TipoRepeticao Repeticao { get; set; } = TipoRepeticao.Nenhuma;
    public int? ParcelaAtual { get; set; }
    public int? ParcelaTotal { get; set; }

    public Guid? ContaId { get; set; }
    public Conta? Conta { get; set; }

    public Guid? CartaoId { get; set; }
    public Cartao? Cartao { get; set; }

    /// <summary>
    /// Mês_Ano de referência: "Data Conciliado" para CC conciliado, "Venc. Fatura" para CD
    /// (ver CLAUDE.md > Regras de Negócio > Metas de orçamento, item 3).
    /// </summary>
    public MesAno? ObterMesAnoReferencia()
    {
        if (Origem == OrigemTransacao.ContaCorrente && Status == StatusTransacao.Conciliado && DataConciliado is not null)
        {
            return MesAno.FromDate(DataConciliado.Value);
        }

        if (Origem == OrigemTransacao.CartaoCredito && VencimentoFatura is not null)
        {
            return MesAno.FromDate(VencimentoFatura.Value);
        }

        return null;
    }

    /// <summary>Entra no cálculo de Gasto_Real de metas (ver CLAUDE.md > Metas de orçamento, itens 1-2).</summary>
    public bool ElegivelParaGastoReal =>
        Status == StatusTransacao.Conciliado
        && Tipo == TipoTransacao.Despesa;
}
