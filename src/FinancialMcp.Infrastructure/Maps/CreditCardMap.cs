using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using FinancialMcp.Domain.Enums;

namespace FinancialMcp.Infrastructure.Maps;

/// <summary>
/// CsvHelper mapping for credit-card statement rows — extends AccountMap with the
/// billing-cycle/installment columns (see CLAUDE.md > Business Rules > Credit card —
/// billing cycle, installments, and projection).
/// </summary>
internal sealed class CreditCardMap : AccountMap
{
    public CreditCardMap()
    {
        Map(m => m.InvoiceDueDate).Name("Venc. Fatura").TypeConverter<OptionalDdMmYyyyDateConverter>();
        Map(m => m.Recurrence).Name("Repetição", "Repeticao").TypeConverter<RecurrenceConverter>();
        Map(m => m.CurrentInstallment).Name("Parcela Atual").TypeConverter<InstallmentNumberConverter>();
        Map(m => m.TotalInstallments).Name("Parcela Total").TypeConverter<InstallmentNumberConverter>();
    }
}

internal sealed class RecurrenceConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        return text switch
        {
            "Parcelado" => RecurrenceType.Installment,
            "Fixo Mês" or "Fixo Mes" => RecurrenceType.FixedMonthly,
            _ => RecurrenceType.None
        };
    }
}

/// <summary>
/// Parcela Atual / Parcela Total: only meaningful when Repetição = "Parcelado" (see
/// CLAUDE.md > Business Rules, item 2) — for any other row these columns are ignored
/// even if present, matching the original MapRow behavior.
/// </summary>
internal sealed class InstallmentNumberConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        var recurrence = row.GetField("Repetição") ?? row.GetField("Repeticao");
        if (recurrence != "Parcelado" || text is null)
        {
            return null;
        }

        return int.Parse(text, CultureInfo.InvariantCulture);
    }
}
