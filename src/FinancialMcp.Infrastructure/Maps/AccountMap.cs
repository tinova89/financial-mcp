using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;

namespace FinancialMcp.Infrastructure.Maps;

/// <summary>
/// CsvHelper mapping for checking-account statement rows (";" separator, "dd/mm/yyyy"
/// dates, dot-decimal — see CLAUDE.md > Code Conventions). Mirrors the field-by-field
/// rules that used to live in StatementCsvParser.MapRow. CreditCardMap extends this map
/// with the credit-card-only columns (see CLAUDE.md > Business Rules > Credit card).
/// </summary>
internal class AccountMap : ClassMap<Transaction>
{
    public AccountMap()
    {
        Map(m => m.Type).Name("Tipo").TypeConverter<TransactionTypeConverter>();
        Map(m => m.Status).Name("Status").TypeConverter<TransactionStatusConverter>();
        Map(m => m.Description).Name("Descrição", "Descricao").Default(string.Empty);
        Map(m => m.Amount).Name("Valor").TypeConverter<AmountConverter>();
        Map(m => m.RawCategory).Name("Categoria").Default("Sem categoria");
        Map(m => m.ExpectedDate).Name("Data prevista").TypeConverter<RequiredDdMmYyyyDateConverter>();
        Map(m => m.ActualDate).Name("Data efetiva").TypeConverter<OptionalDdMmYyyyDateConverter>();

        // Checking-account only (see CLAUDE.md > Budget goals, item 3). Harmless for
        // credit-card rows: the "Data Conciliado" column simply doesn't exist there,
        // so with MissingFieldFound suppressed this just stays null. The source column
        // header stays "Data Conciliado"; only the mapped domain property is renamed.
        Map(m => m.ConfirmationDate).Name("Data Conciliado").TypeConverter<OptionalDdMmYyyyDateConverter>();
    }
}

internal sealed class TransactionTypeConverter : DefaultTypeConverter
{
    private static readonly Dictionary<string, TransactionType> TypeByCsvValue = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Despesa"] = TransactionType.Expense,
        ["Receita"] = TransactionType.Income,
        ["Transferencia"] = TransactionType.Transfer,
        ["Pagamento"] = TransactionType.Payment
    };

    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text is null)
        {
            throw new FormatException("Campo 'Tipo' ausente.");
        }

        return TypeByCsvValue.TryGetValue(text, out var type)
            ? type
            : throw new FormatException($"Valor de 'Tipo' desconhecido: '{text}'.");
    }
}

internal sealed class TransactionStatusConverter : DefaultTypeConverter
{
    // Statement status text → Card #14 status: "Conciliado" → Confirmed,
    // "Agendado"/"Nconciliado" → Scheduled. import_statement behaviour is otherwise unchanged.
    private static readonly Dictionary<string, TransactionStatus> StatusByCsvValue = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Conciliado"] = TransactionStatus.Confirmed,
        ["Agendado"] = TransactionStatus.Scheduled,
        ["Nconciliado"] = TransactionStatus.Scheduled
    };

    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text is null)
        {
            throw new FormatException("Campo 'Status' ausente.");
        }

        return StatusByCsvValue.TryGetValue(text, out var status)
            ? status
            : throw new FormatException($"Valor de 'Status' desconhecido: '{text}'.");
    }
}

internal sealed class AmountConverter : DefaultTypeConverter
{
    // Statements use comma as the decimal separator (e.g. "-70,00") and dot as the
    // thousands separator (pt-BR formatting), not InvariantCulture's dot-decimal.
    private static readonly NumberFormatInfo DecimalCommaFormat = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "."
    };

    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text is null)
        {
            throw new FormatException("Campo 'Valor' ausente.");
        }

        return decimal.Parse(text, NumberStyles.Number, DecimalCommaFormat);
    }
}

/// <summary>Required date column: missing/unparseable throws, matching the original "Campo ... inválido ou ausente." rule.</summary>
internal sealed class RequiredDdMmYyyyDateConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (!string.IsNullOrWhiteSpace(text)
            && DateOnly.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        var fieldName = memberMapData.Names.FirstOrDefault() ?? memberMapData.Member?.Name;
        throw new FormatException($"Campo '{fieldName}' inválido ou ausente.");
    }
}

/// <summary>Optional date column: missing/unparseable silently yields null, same as the original ParseDdMmYyyyDate.</summary>
internal sealed class OptionalDdMmYyyyDateConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return DateOnly.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
