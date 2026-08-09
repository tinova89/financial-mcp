using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Common.Services;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FinancialMcp.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.BalanceProjection.GetBalanceProjection;

/// <summary>
/// Single handler for GetBalanceProjectionQuery. Implements CLAUDE.md > Business
/// Rules > Credit card — billing cycle, installments and projection:
///  1. Always uses "Venc. Fatura" (never "Data prevista") as the date that impacts the balance.
///  2. Generates remaining installments for "Parcelado" entries not yet present in the statement.
///  3. Repeats "Fixo Mês" entries every month.
///  4. Consolidates the card bill into a single "Pagamento de cartão" entry in the
///     checking account, on the due date (adjusted to the next business day).
/// </summary>
public sealed class GetBalanceProjectionQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBalanceProjectionQuery, IReadOnlyList<MonthlyProjectionDto>>
{
    public async Task<IReadOnlyList<MonthlyProjectionDto>> Handle(GetBalanceProjectionQuery request, CancellationToken cancellationToken)
    {
        var cards = await db.Cards.AsNoTracking()
            .Where(c => c.ContaId == request.AccountId)
            .ToListAsync(cancellationToken);

        var cardIds = cards.Select(c => c.Id).ToHashSet();

        var cardEntries = await db.Transactions.AsNoTracking()
            .Where(t => t.Origem == OrigemTransacao.CartaoCredito && t.CartaoId != null && cardIds.Contains(t.CartaoId!.Value))
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startMonth = MesAno.FromDate(today);

        var result = new List<MonthlyProjectionDto>(request.MonthsAhead);

        for (var i = 0; i < request.MonthsAhead; i++)
        {
            var monthYear = startMonth.AdicionarMeses(i);

            var monthEntries = new List<ProjectedEntryDto>();

            // 1) Entries already present in the statement for this Mês_Ano (Venc. Fatura).
            var existingEntries = cardEntries
                .Where(t => t.VencimentoFatura is not null && MesAno.FromDate(t.VencimentoFatura.Value) == monthYear)
                .ToList();

            foreach (var t in existingEntries)
            {
                var label = t.Repeticao == TipoRepeticao.Parcelado && t.ParcelaAtual is not null && t.ParcelaTotal is not null
                    ? $"{t.ParcelaAtual}/{t.ParcelaTotal}"
                    : null;

                monthEntries.Add(new ProjectedEntryDto(t.Descricao, t.Valor, t.VencimentoFatura!.Value, Projected: false, label));
            }

            // 2) Remaining installments of "Parcelado" entries whose installment for this month doesn't exist yet in the statement.
            foreach (var group in cardEntries
                         .Where(t => t.Repeticao == TipoRepeticao.Parcelado && t.ParcelaAtual is not null && t.ParcelaTotal is not null)
                         .GroupBy(t => new { t.CartaoId, BaseDescription = RemoveInstallmentSuffix(t.Descricao) }))
            {
                var lastKnownInstallment = group.OrderByDescending(t => t.ParcelaAtual).First();
                var projectedInstallment = lastKnownInstallment.ParcelaAtual!.Value + InstallmentsAlreadyGeneratedUntil(lastKnownInstallment, monthYear);

                if (projectedInstallment > lastKnownInstallment.ParcelaTotal!.Value)
                {
                    continue; // installment plan already finished
                }

                var baseDueDate = lastKnownInstallment.VencimentoFatura;
                if (baseDueDate is null)
                {
                    continue;
                }

                var lastKnownMonth = MesAno.FromDate(baseDueDate.Value);
                if (monthYear.Ano * 12 + monthYear.Mes <= lastKnownMonth.Ano * 12 + lastKnownMonth.Mes)
                {
                    continue; // this month is already covered by an existing entry
                }

                var alreadyExistsThisMonth = existingEntries.Any(t =>
                    t.CartaoId == lastKnownInstallment.CartaoId &&
                    RemoveInstallmentSuffix(t.Descricao) == RemoveInstallmentSuffix(lastKnownInstallment.Descricao));

                if (alreadyExistsThisMonth)
                {
                    continue;
                }

                var projectedDueDate = baseDueDate.Value.AddMonths(monthYear.Ano * 12 + monthYear.Mes - (lastKnownMonth.Ano * 12 + lastKnownMonth.Mes));

                monthEntries.Add(new ProjectedEntryDto(
                    RemoveInstallmentSuffix(lastKnownInstallment.Descricao),
                    lastKnownInstallment.Valor,
                    projectedDueDate,
                    Projected: true,
                    $"{projectedInstallment}/{lastKnownInstallment.ParcelaTotal}"));
            }

            // 3) "Fixo Mês" entries — repeat every month until an end is indicated (not modeled in the MVP: repeats indefinitely).
            foreach (var fixedEntry in cardEntries.Where(t => t.Repeticao == TipoRepeticao.FixoMes))
            {
                var alreadyExistsThisMonth = existingEntries.Any(t => t.CartaoId == fixedEntry.CartaoId && t.Descricao == fixedEntry.Descricao);
                if (alreadyExistsThisMonth || fixedEntry.VencimentoFatura is null)
                {
                    continue;
                }

                var fixedEntryMonth = MesAno.FromDate(fixedEntry.VencimentoFatura.Value);
                if (monthYear.Ano * 12 + monthYear.Mes <= fixedEntryMonth.Ano * 12 + fixedEntryMonth.Mes)
                {
                    continue;
                }

                var diff = monthYear.Ano * 12 + monthYear.Mes - (fixedEntryMonth.Ano * 12 + fixedEntryMonth.Mes);
                monthEntries.Add(new ProjectedEntryDto(
                    fixedEntry.Descricao, fixedEntry.Valor, fixedEntry.VencimentoFatura.Value.AddMonths(diff), Projected: true, InstallmentLabel: null));
            }

            // 4) Consolidation: sum of the bill -> single "Pagamento de cartão" entry in the account,
            //    on the due date, adjusted to the next business day (never the raw bill in the checking account).
            var totalBill = monthEntries.Sum(l => Math.Abs(l.Amount));
            var cardDueDate = monthEntries.Count > 0
                ? BusinessDayHelper.NextBusinessDay(monthEntries.Max(l => l.EntryDate))
                : (DateOnly?)null;

            result.Add(new MonthlyProjectionDto(monthYear.Ano, monthYear.Mes, monthEntries, totalBill, cardDueDate));
        }

        return result;
    }

    private static string RemoveInstallmentSuffix(string description)
    {
        // Removes the "N/M" suffix from the base description (e.g. "Notebook 6/12" -> "Notebook").
        var idx = description.LastIndexOf(' ');
        if (idx < 0) return description;

        var possibleSuffix = description[(idx + 1)..];
        return possibleSuffix.Contains('/') ? description[..idx] : description;
    }

    private static int InstallmentsAlreadyGeneratedUntil(Transacao lastKnownInstallment, MesAno targetMonth)
    {
        if (lastKnownInstallment.VencimentoFatura is null) return 0;

        var baseMonth = MesAno.FromDate(lastKnownInstallment.VencimentoFatura.Value);
        var diff = (targetMonth.Ano * 12 + targetMonth.Mes) - (baseMonth.Ano * 12 + baseMonth.Mes);
        return Math.Max(diff, 0);
    }
}
