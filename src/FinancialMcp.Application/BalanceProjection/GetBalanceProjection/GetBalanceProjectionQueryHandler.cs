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
        var cards = await db.CreditCards.AsNoTracking()
            .Where(c => c.PaymentAccountId == request.AccountId)
            .ToListAsync(cancellationToken);

        var cardIds = cards.Select(c => c.Id).ToHashSet();

        // No Source check needed: cardIds only contains ids of actual CreditCard rows (Account.Kind
        // == Credit by construction), so AccountId membership alone is the authoritative signal —
        // no need to also trust the separately-supplied Source flag.
        var cardEntries = await db.Transactions.AsNoTracking()
            .Where(t => cardIds.Contains(t.AccountId))
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startMonth = MonthYear.FromDate(today);

        var result = new List<MonthlyProjectionDto>(request.MonthsAhead);

        for (var i = 0; i < request.MonthsAhead; i++)
        {
            var monthYear = startMonth.AddMonths(i);

            var monthEntries = new List<ProjectedEntryDto>();

            // 1) Entries already present in the statement for this Mês_Ano (Venc. Fatura).
            var existingEntries = cardEntries
                .Where(t => t.InvoiceDueDate is not null && MonthYear.FromDate(t.InvoiceDueDate.Value) == monthYear)
                .ToList();

            foreach (var t in existingEntries)
            {
                var label = t.Recurrence == RecurrenceType.Installment && t.CurrentInstallment is not null && t.TotalInstallments is not null
                    ? $"{t.CurrentInstallment}/{t.TotalInstallments}"
                    : null;

                monthEntries.Add(new ProjectedEntryDto(t.Description, t.Amount, t.InvoiceDueDate!.Value, Projected: false, label));
            }

            // 2) Remaining installments of "Parcelado" entries whose installment for this month doesn't exist yet in the statement.
            foreach (var group in cardEntries
                         .Where(t => t.Recurrence == RecurrenceType.Installment && t.CurrentInstallment is not null && t.TotalInstallments is not null)
                         .GroupBy(t => new { t.AccountId, BaseDescription = RemoveInstallmentSuffix(t.Description) }))
            {
                var lastKnownInstallment = group.OrderByDescending(t => t.CurrentInstallment).First();
                var projectedInstallment = lastKnownInstallment.CurrentInstallment!.Value + InstallmentsAlreadyGeneratedUntil(lastKnownInstallment, monthYear);

                if (projectedInstallment > lastKnownInstallment.TotalInstallments!.Value)
                {
                    continue; // installment plan already finished
                }

                var baseDueDate = lastKnownInstallment.InvoiceDueDate;
                if (baseDueDate is null)
                {
                    continue;
                }

                var lastKnownMonth = MonthYear.FromDate(baseDueDate.Value);
                if (monthYear.Year * 12 + monthYear.Month <= lastKnownMonth.Year * 12 + lastKnownMonth.Month)
                {
                    continue; // this month is already covered by an existing entry
                }

                var alreadyExistsThisMonth = existingEntries.Any(t =>
                    t.AccountId == lastKnownInstallment.AccountId &&
                    RemoveInstallmentSuffix(t.Description) == RemoveInstallmentSuffix(lastKnownInstallment.Description));

                if (alreadyExistsThisMonth)
                {
                    continue;
                }

                var projectedDueDate = baseDueDate.Value.AddMonths(monthYear.Year * 12 + monthYear.Month - (lastKnownMonth.Year * 12 + lastKnownMonth.Month));

                monthEntries.Add(new ProjectedEntryDto(
                    RemoveInstallmentSuffix(lastKnownInstallment.Description),
                    lastKnownInstallment.Amount,
                    projectedDueDate,
                    Projected: true,
                    $"{projectedInstallment}/{lastKnownInstallment.TotalInstallments}"));
            }

            // 3) "Fixo Mês" entries — repeat every month until an end is indicated (not modeled in the MVP: repeats indefinitely).
            foreach (var fixedEntry in cardEntries.Where(t => t.Recurrence == RecurrenceType.FixedMonthly))
            {
                var alreadyExistsThisMonth = existingEntries.Any(t => t.AccountId == fixedEntry.AccountId && t.Description == fixedEntry.Description);
                if (alreadyExistsThisMonth || fixedEntry.InvoiceDueDate is null)
                {
                    continue;
                }

                var fixedEntryMonth = MonthYear.FromDate(fixedEntry.InvoiceDueDate.Value);
                if (monthYear.Year * 12 + monthYear.Month <= fixedEntryMonth.Year * 12 + fixedEntryMonth.Month)
                {
                    continue;
                }

                var diff = monthYear.Year * 12 + monthYear.Month - (fixedEntryMonth.Year * 12 + fixedEntryMonth.Month);
                monthEntries.Add(new ProjectedEntryDto(
                    fixedEntry.Description, fixedEntry.Amount, fixedEntry.InvoiceDueDate.Value.AddMonths(diff), Projected: true, InstallmentLabel: null));
            }

            // 4) Consolidation: sum of the bill -> single "Pagamento de cartão" entry in the account,
            //    on the due date, adjusted to the next business day (never the raw bill in the checking account).
            var totalBill = monthEntries.Sum(l => Math.Abs(l.Amount));
            var cardDueDate = monthEntries.Count > 0
                ? BusinessDayHelper.NextBusinessDay(monthEntries.Max(l => l.EntryDate))
                : (DateOnly?)null;

            result.Add(new MonthlyProjectionDto(monthYear.Year, monthYear.Month, monthEntries, totalBill, cardDueDate));
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

    private static int InstallmentsAlreadyGeneratedUntil(Transaction lastKnownInstallment, MonthYear targetMonth)
    {
        if (lastKnownInstallment.InvoiceDueDate is null) return 0;

        var baseMonth = MonthYear.FromDate(lastKnownInstallment.InvoiceDueDate.Value);
        var diff = (targetMonth.Year * 12 + targetMonth.Month) - (baseMonth.Year * 12 + baseMonth.Month);
        return Math.Max(diff, 0);
    }
}
