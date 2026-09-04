using FinancialMcp.Domain.Enums;
using FinancialMcp.Infrastructure.Statements;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — the real <c>StatementCsvParser</c> (";" separator, "dd/mm/yyyy" dates,
/// dot-decimal already parsed into decimal via comma-decimal statement values). Complements
/// <c>ImportStatementCommandHandlerTests</c>, which mocks <c>IStatementCsvParser</c> and
/// covers handler-level routing/atomicity instead of the parser's own CSV-format edge cases.
/// </summary>
public class StatementCsvParserTests
{
    private static readonly string[] AccountColumns =
        ["Tipo", "Status", "Descrição", "Valor", "Categoria", "Data prevista", "Data efetiva", "Data Conciliado"];

    private static readonly string[] CreditCardColumns =
        [.. AccountColumns, "Venc. Fatura", "Repetição", "Parcela Atual", "Parcela Total"];

    private static string Row(params string[] fields) => string.Join(";", fields);

    private static string Csv(string[] columns, params string[] rows) =>
        string.Join("|", new[] { Row(columns) }.Concat(rows));

    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public void A_wrong_delimiter_silently_produces_a_default_valued_row_without_a_warning()
    {
        // Actual current behavior — NOT what StatementCsvParser's own doc comment promises
        // ("Invalid lines generate a warning and are skipped"). With Delimiter = ";"
        // configured but a comma-separated file, the whole line becomes a single column
        // that matches none of the mapped header names. MissingFieldFound = null makes
        // CsvHelper silently substitute default(T) for every "missing" property WITHOUT
        // ever invoking the custom TypeConverters (they only run for a field CsvHelper
        // actually finds) — so instead of a warning or an exception, a garbage all-default
        // Transaction is produced (Type/Status = 0, an invalid enum value; Amount = 0;
        // ExpectedDate = 0001-01-01). Locked in as documented current behavior — flagged
        // separately (see the Card #20 summary) as a real gap between this parser's doc
        // comment and its actual behavior; not fixed here (tests-only card).
        var validCsv = Csv(AccountColumns, Row("Despesa", "Agendado", "Compra", "-50,00", "Mercado", "01/06/2026", "", ""));
        var commaSeparated = validCsv.Replace(';', ',');

        var parser = new StatementCsvParser();
        var transactions = parser.Parse(commaSeparated, isCreditCard: false, AccountId, out var warnings);

        warnings.Should().BeEmpty();
        var transaction = transactions.Should().ContainSingle().Subject;
        transaction.Type.Should().Be((TransactionType)0);
        transaction.Amount.Should().Be(0m);
        transaction.ExpectedDate.Should().Be(default(DateOnly));
    }

    [Fact]
    public void A_missing_required_expected_date_produces_a_warning_and_skips_only_that_row()
    {
        var csv = Csv(
            AccountColumns,
            Row("Despesa", "Agendado", "Valido", "-10,00", "Mercado", "01/06/2026", "", ""),
            Row("Despesa", "Agendado", "Invalido", "-20,00", "Mercado", "", "", ""));

        var parser = new StatementCsvParser();
        var transactions = parser.Parse(csv, isCreditCard: false, AccountId, out var warnings);

        transactions.Should().ContainSingle(t => t.Description == "Valido");
        warnings.Should().ContainSingle();
    }

    [Fact]
    public void An_unparseable_optional_date_silently_imports_the_row_with_that_field_null()
    {
        var csv = Csv(
            AccountColumns,
            Row("Despesa", "Agendado", "Compra", "-10,00", "Mercado", "01/06/2026", "data-invalida", ""));

        var parser = new StatementCsvParser();
        var transactions = parser.Parse(csv, isCreditCard: false, AccountId, out var warnings);

        var transaction = transactions.Should().ContainSingle().Subject;
        transaction.ActualDate.Should().BeNull();
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void A_bad_row_silently_drops_every_row_after_it_even_though_only_the_bad_row_itself_warns()
    {
        // Actual current behavior — also NOT what the doc comment promises ("to allow
        // partial import with feedback to the caller"). GetRecords<T>() is a lazy,
        // compiler-generated iterator; the parser's per-row try/catch swallows the
        // exception and calls MoveNext() again to continue, but a C# iterator that throws
        // out of one MoveNext() call becomes exhausted — the next MoveNext() just returns
        // false instead of resuming at the following row. So every row after the first bad
        // one is silently dropped (no warning, no exception for them), not just the bad row
        // itself. Locked in as documented current behavior — flagged separately (see the
        // Card #20 summary) as a real partial-import gap; not fixed here (tests-only card).
        var csv = Csv(
            AccountColumns,
            Row("Despesa", "Agendado", "Primeira", "-10,00", "Mercado", "01/06/2026", "", ""),
            Row("Despesa", "Agendado", "Invalida", "nao-e-um-valor", "Mercado", "01/06/2026", "", ""),
            Row("Despesa", "Agendado", "Terceira", "-30,00", "Mercado", "03/06/2026", "", ""));

        var parser = new StatementCsvParser();
        var transactions = parser.Parse(csv, isCreditCard: false, AccountId, out var warnings);

        transactions.Should().ContainSingle(t => t.Description == "Primeira");
        warnings.Should().ContainSingle();
    }

    [Fact]
    public void Routing_true_engages_the_credit_card_map()
    {
        var csv = Csv(
            CreditCardColumns,
            Row("Despesa", "Agendado", "Compra parcelada", "-90,00", "Eletronicos", "01/06/2026", "", "",
                "10/06/2026", "Parcelado", "2", "3"));

        var parser = new StatementCsvParser();
        var transactions = parser.Parse(csv, isCreditCard: true, AccountId, out var warnings);

        warnings.Should().BeEmpty();
        var transaction = transactions.Should().ContainSingle().Subject;
        transaction.InvoiceDueDate.Should().Be(new DateOnly(2026, 6, 10));
        transaction.Recurrence.Should().Be(RecurrenceType.Installment);
        transaction.CurrentInstallment.Should().Be(2);
        transaction.TotalInstallments.Should().Be(3);
        transaction.AccountId.Should().Be(AccountId);
    }

    [Fact]
    public void Routing_false_engages_the_account_map()
    {
        var csv = Csv(
            AccountColumns,
            Row("Despesa", "Conciliado", "Compra", "-15,00", "Mercado", "01/06/2026", "", "02/06/2026"));

        var parser = new StatementCsvParser();
        var transactions = parser.Parse(csv, isCreditCard: false, AccountId, out var warnings);

        warnings.Should().BeEmpty();
        var transaction = transactions.Should().ContainSingle().Subject;
        transaction.ConfirmationDate.Should().Be(new DateOnly(2026, 6, 2));
        transaction.Recurrence.Should().Be(RecurrenceType.None, "AccountMap never maps Recurrence — it stays at the entity's default");
        transaction.InvoiceDueDate.Should().BeNull();
    }
}
