using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Common.Services;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.BalanceProjection.GetBalanceProjection;

/// <summary>
/// Handler único para GetBalanceProjectionQuery. Implementa CLAUDE.md > Regras de
/// Negócio > Cartão de crédito — ciclo de fatura, parcelamento e projeção:
///  1. Usa sempre "Venc. Fatura" (nunca "Data prevista") como data que impacta o saldo.
///  2. Gera parcelas restantes de lançamentos "Parcelado" ainda não presentes no extrato.
///  3. Repete lançamentos "Fixo Mês" todo mês.
///  4. Consolida a fatura do cartão em um único lançamento "Pagamento de cartão" na
///     conta corrente, na data de vencimento (ajustada para o próximo dia útil).
/// </summary>
public sealed class GetBalanceProjectionQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBalanceProjectionQuery, IReadOnlyList<MonthlyProjectionDto>>
{
    public async Task<IReadOnlyList<MonthlyProjectionDto>> Handle(GetBalanceProjectionQuery request, CancellationToken cancellationToken)
    {
        var cartoes = await db.Cartoes.AsNoTracking()
            .Where(c => c.ContaId == request.AccountId)
            .ToListAsync(cancellationToken);

        var cartaoIds = cartoes.Select(c => c.Id).ToHashSet();

        var lancamentosCartao = await db.Transacoes.AsNoTracking()
            .Where(t => t.Origem == OrigemTransacao.CartaoCredito && t.CartaoId != null && cartaoIds.Contains(t.CartaoId!.Value))
            .ToListAsync(cancellationToken);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var mesInicial = MesAno.FromDate(hoje);

        var resultado = new List<MonthlyProjectionDto>(request.MonthsAhead);

        for (var i = 0; i < request.MonthsAhead; i++)
        {
            var mesAno = mesInicial.AdicionarMeses(i);

            var lancamentosDoMes = new List<ProjectedEntryDto>();

            // 1) Lançamentos já existentes no extrato para este Mês_Ano (Venc. Fatura).
            var existentes = lancamentosCartao
                .Where(t => t.VencimentoFatura is not null && MesAno.FromDate(t.VencimentoFatura.Value) == mesAno)
                .ToList();

            foreach (var t in existentes)
            {
                var label = t.Repeticao == TipoRepeticao.Parcelado && t.ParcelaAtual is not null && t.ParcelaTotal is not null
                    ? $"{t.ParcelaAtual}/{t.ParcelaTotal}"
                    : null;

                lancamentosDoMes.Add(new ProjectedEntryDto(t.Descricao, t.Valor, t.VencimentoFatura!.Value, Projected: false, label));
            }

            // 2) Parcelas restantes de "Parcelado" cuja parcela deste mês ainda não existe no extrato.
            foreach (var grupo in lancamentosCartao
                         .Where(t => t.Repeticao == TipoRepeticao.Parcelado && t.ParcelaAtual is not null && t.ParcelaTotal is not null)
                         .GroupBy(t => new { t.CartaoId, DescricaoBase = RemoverSufixoParcela(t.Descricao) }))
            {
                var ultimaParcelaConhecida = grupo.OrderByDescending(t => t.ParcelaAtual).First();
                var parcelaProjetada = ultimaParcelaConhecida.ParcelaAtual!.Value + ParcelasJaGeradasAte(ultimaParcelaConhecida, mesAno);

                if (parcelaProjetada > ultimaParcelaConhecida.ParcelaTotal!.Value)
                {
                    continue; // parcelamento já encerrado
                }

                var vencimentoBase = ultimaParcelaConhecida.VencimentoFatura;
                if (vencimentoBase is null)
                {
                    continue;
                }

                var mesDaUltimaConhecida = MesAno.FromDate(vencimentoBase.Value);
                if (mesAno.Ano * 12 + mesAno.Mes <= mesDaUltimaConhecida.Ano * 12 + mesDaUltimaConhecida.Mes)
                {
                    continue; // este mês já está coberto por um lançamento existente
                }

                var jaExisteNesteMes = existentes.Any(t =>
                    t.CartaoId == ultimaParcelaConhecida.CartaoId &&
                    RemoverSufixoParcela(t.Descricao) == RemoverSufixoParcela(ultimaParcelaConhecida.Descricao));

                if (jaExisteNesteMes)
                {
                    continue;
                }

                var vencimentoProjetado = vencimentoBase.Value.AddMonths(mesAno.Ano * 12 + mesAno.Mes - (mesDaUltimaConhecida.Ano * 12 + mesDaUltimaConhecida.Mes));

                lancamentosDoMes.Add(new ProjectedEntryDto(
                    RemoverSufixoParcela(ultimaParcelaConhecida.Descricao),
                    ultimaParcelaConhecida.Valor,
                    vencimentoProjetado,
                    Projected: true,
                    $"{parcelaProjetada}/{ultimaParcelaConhecida.ParcelaTotal}"));
            }

            // 3) Lançamentos "Fixo Mês" — repete todo mês até indicação de término (não modelada no MVP: repete indefinidamente).
            foreach (var fixo in lancamentosCartao.Where(t => t.Repeticao == TipoRepeticao.FixoMes))
            {
                var jaExisteNesteMes = existentes.Any(t => t.CartaoId == fixo.CartaoId && t.Descricao == fixo.Descricao);
                if (jaExisteNesteMes || fixo.VencimentoFatura is null)
                {
                    continue;
                }

                var mesDoFixo = MesAno.FromDate(fixo.VencimentoFatura.Value);
                if (mesAno.Ano * 12 + mesAno.Mes <= mesDoFixo.Ano * 12 + mesDoFixo.Mes)
                {
                    continue;
                }

                var diff = mesAno.Ano * 12 + mesAno.Mes - (mesDoFixo.Ano * 12 + mesDoFixo.Mes);
                lancamentosDoMes.Add(new ProjectedEntryDto(
                    fixo.Descricao, fixo.Valor, fixo.VencimentoFatura.Value.AddMonths(diff), Projected: true, InstallmentLabel: null));
            }

            // 4) Consolidação: soma da fatura -> único "Pagamento de cartão" na conta,
            //    na data de vencimento, ajustada para o próximo dia útil (nunca a fatura crua na CC).
            var totalFatura = lancamentosDoMes.Sum(l => Math.Abs(l.Amount));
            var dataVencimentoCartao = lancamentosDoMes.Count > 0
                ? BusinessDayHelper.ProximoDiaUtil(lancamentosDoMes.Max(l => l.EntryDate))
                : (DateOnly?)null;

            resultado.Add(new MonthlyProjectionDto(mesAno.Ano, mesAno.Mes, lancamentosDoMes, totalFatura, dataVencimentoCartao));
        }

        return resultado;
    }

    private static string RemoverSufixoParcela(string descricao)
    {
        // Remove sufixo "N/M" da descrição-base (ex.: "Notebook 6/12" -> "Notebook").
        var idx = descricao.LastIndexOf(' ');
        if (idx < 0) return descricao;

        var possivelSufixo = descricao[(idx + 1)..];
        return possivelSufixo.Contains('/') ? descricao[..idx] : descricao;
    }

    private static int ParcelasJaGeradasAte(Transacao ultimaParcelaConhecida, MesAno mesAlvo)
    {
        if (ultimaParcelaConhecida.VencimentoFatura is null) return 0;

        var mesBase = MesAno.FromDate(ultimaParcelaConhecida.VencimentoFatura.Value);
        var diff = (mesAlvo.Ano * 12 + mesAlvo.Mes) - (mesBase.Ano * 12 + mesBase.Mes);
        return Math.Max(diff, 0);
    }
}
