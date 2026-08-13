using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FinancialMcp.Application.Statements.ImportStatement;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Infrastructure.Maps;

namespace FinancialMcp.Infrastructure.Statements;

/// <summary>
/// Concrete parser for the statement format: ";" separator, "dd/mm/yyyy" dates,
/// dot-decimal (see CLAUDE.md > MCP and Code Conventions). Invalid lines generate
/// a warning and are skipped — they never throw an exception that aborts the
/// whole batch, to allow partial import with feedback to the caller. Row-to-entity
/// mapping rules live in FinancialMcp.Infrastructure.Maps (AccountMap / CreditCardMap),
/// selected here based on the statement's source.
/// </summary>
public sealed class StatementCsvParser : IStatementCsvParser
{
    public IReadOnlyList<Transaction> Parse(
        string csvContent, bool isCreditCard, Guid accountId, out IReadOnlyList<string> warnings)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            NewLine = "|",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            // A mapped column absent from the file entirely (e.g. "Data Conciliado" on a
            // credit-card statement) must behave like the original csv.GetField(...) ??
            // null — not throw. Per-row short lines are separately covered by MissingFieldFound.
            HeaderValidated = null,
        };

        var transactions = new List<Transaction>();
        var warningList = new List<string>();

        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, config);

        if (isCreditCard)
        {
            csv.Context.RegisterClassMap<CreditCardMap>();
        }
        else
        {
            csv.Context.RegisterClassMap<AccountMap>();
        }

        using var records = csv.GetRecords<Transaction>().GetEnumerator();

        while (true)
        {
            Transaction transaction;

            try
            {
                if (!records.MoveNext())
                {
                    break;
                }

                transaction = records.Current;
            }
            catch (Exception ex)
            {
                warningList.Add($"Linha {csv.Context.Parser?.Row}: {ex.Message}");
                continue;
            }

            transaction.AccountId = accountId;
            transactions.Add(transaction);
        }

        warnings = warningList;
        return transactions;
    }
}
