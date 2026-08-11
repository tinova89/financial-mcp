using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FinancialMcp.Application.Statements.ImportStatement;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;

namespace FinancialMcp.Infrastructure.Statements;

/// <summary>
/// Concrete parser for the statement format: ";" separator, "dd/mm/yyyy" dates,
/// dot-decimal (see CLAUDE.md > MCP and Code Conventions). Invalid lines generate
/// a warning and are skipped — they never throw an exception that aborts the
/// whole batch, to allow partial import with feedback to the caller.
/// </summary>
public sealed class StatementCsvParser : IStatementCsvParser
{
    // Real-world statement files still carry Portuguese "Tipo"/"Status" column text —
    // these map that raw text to the (now English) Domain enum members.
    private static readonly Dictionary<string, TransactionType> TypeByCsvValue = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Despesa"] = TransactionType.Expense,
        ["Receita"] = TransactionType.Income,
        ["Transferencia"] = TransactionType.Transfer,
        ["Pagamento"] = TransactionType.Payment
    };

    private static readonly Dictionary<string, TransactionStatus> StatusByCsvValue = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Conciliado"] = TransactionStatus.Reconciled,
        ["Agendado"] = TransactionStatus.Scheduled,
        ["Nconciliado"] = TransactionStatus.Unreconciled
    };

    public IReadOnlyList<Transaction> Parse(
        string csvContent, bool isCreditCard, Guid accountId, out IReadOnlyList<string> warnings)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        var transactions = new List<Transaction>();
        var warningList = new List<string>();

        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        var line = 1;

        while (csv.Read())
        {
            line++;

            try
            {
                var transaction = MapRow(csv, isCreditCard, accountId);
                transactions.Add(transaction);
            }
            catch (Exception ex)
            {
                warningList.Add($"Linha {line}: {ex.Message}");
            }
        }

        warnings = warningList;
        return transactions;
    }

    private static Transaction MapRow(CsvReader csv, bool isCreditCard, Guid accountId)
    {
        var rawAmount = csv.GetField("Valor") ?? throw new FormatException("Campo 'Valor' ausente.");
        var amount = decimal.Parse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture);

        var rawType = csv.GetField("Tipo") ?? throw new FormatException("Campo 'Tipo' ausente.");
        var rawStatus = csv.GetField("Status") ?? throw new FormatException("Campo 'Status' ausente.");
        var category = csv.GetField("Categoria") ?? "Sem categoria";
        var description = csv.GetField("Descrição") ?? csv.GetField("Descricao") ?? string.Empty;

        var expectedDate = ParseDdMmYyyyDate(csv.GetField("Data prevista"))
            ?? throw new FormatException("Campo 'Data prevista' inválido ou ausente.");

        var transaction = new Transaction
        {
            Type = ResolveType(rawType),
            Status = ResolveStatus(rawStatus),
            Description = description,
            Amount = amount,
            RawCategory = category,
            ExpectedDate = expectedDate,
            ActualDate = ParseDdMmYyyyDate(csv.GetField("Data efetiva")),
            AccountId = accountId
        };

        if (!isCreditCard)
        {
            transaction.ReconciledDate = ParseDdMmYyyyDate(csv.GetField("Data Conciliado"));
        }
        else
        {
            transaction.InvoiceDueDate = ParseDdMmYyyyDate(csv.GetField("Venc. Fatura"));

            var rawRecurrence = csv.GetField("Repetição") ?? csv.GetField("Repeticao");
            transaction.Recurrence = rawRecurrence switch
            {
                "Parcelado" => RecurrenceType.Installment,
                "Fixo Mês" or "Fixo Mes" => RecurrenceType.FixedMonthly,
                _ => RecurrenceType.None
            };

            if (transaction.Recurrence == RecurrenceType.Installment)
            {
                var installment = csv.GetField("Parcela Atual");
                var totalInstallments = csv.GetField("Parcela Total");

                if (installment is not null && totalInstallments is not null)
                {
                    transaction.CurrentInstallment = int.Parse(installment, CultureInfo.InvariantCulture);
                    transaction.TotalInstallments = int.Parse(totalInstallments, CultureInfo.InvariantCulture);
                }
            }
        }

        return transaction;
    }

    private static TransactionType ResolveType(string rawValue) =>
        TypeByCsvValue.TryGetValue(rawValue, out var type)
            ? type
            : throw new FormatException($"Valor de 'Tipo' desconhecido: '{rawValue}'.");

    private static TransactionStatus ResolveStatus(string rawValue) =>
        StatusByCsvValue.TryGetValue(rawValue, out var status)
            ? status
            : throw new FormatException($"Valor de 'Status' desconhecido: '{rawValue}'.");

    private static DateOnly? ParseDdMmYyyyDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
