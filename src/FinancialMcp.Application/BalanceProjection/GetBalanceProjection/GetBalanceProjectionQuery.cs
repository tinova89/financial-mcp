using MediatR;

namespace FinancialMcp.Application.BalanceProjection.GetBalanceProjection;

/// <summary>
/// Corresponde à tool MCP `get_balance_projection`. Gera a projeção de saldo
/// consolidada aplicando ciclo de fatura, parcelamento e lançamentos fixos
/// (ver CLAUDE.md > Regras de Negócio > Cartão de crédito).
/// </summary>
public sealed record GetBalanceProjectionQuery(Guid ContaId, int MesesAFrente = 6)
    : IRequest<IReadOnlyList<ProjecaoMensalDto>>;

public sealed record ProjecaoMensalDto(
    int Ano,
    int Mes,
    IReadOnlyList<LancamentoProjetadoDto> Lancamentos,
    decimal TotalFaturaCartao,
    DateOnly? PagamentoCartaoData);

public sealed record LancamentoProjetadoDto(
    string Descricao,
    decimal Valor,
    DateOnly Data,
    bool Projetado, // true quando gerado pela projeção (parcela futura/fixo), não presente no extrato ainda
    string? ParcelaLabel); // ex.: "7/12"
