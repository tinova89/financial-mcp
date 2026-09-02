using FinancialMcp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #14 — legacy → current <see cref="TransactionStatus"/> remap
/// (<c>Agendado</c>/<c>Nconciliado</c> → <c>Scheduled</c>, <c>Conciliado</c> → <c>Confirmed</c>).
/// </summary>
public class TransactionStatusRemapTests
{
    [Theory]
    [InlineData(TransactionStatusRemap.LegacyReconciled, TransactionStatus.Confirmed)]   // Conciliado
    [InlineData(TransactionStatusRemap.LegacyScheduled, TransactionStatus.Scheduled)]    // Agendado
    [InlineData(TransactionStatusRemap.LegacyUnreconciled, TransactionStatus.Scheduled)] // Nconciliado
    public void FromLegacy_maps_each_old_value_to_the_new_status(int legacy, TransactionStatus expected)
    {
        TransactionStatusRemap.FromLegacy(legacy).Should().Be((int)expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void FromLegacy_leaves_an_out_of_range_value_untouched(int unknown)
    {
        // The legacy numbers 1-3 all carry meaning, so the remap must run exactly once
        // (the migration's single UPDATE). Only genuinely unknown values pass through.
        TransactionStatusRemap.FromLegacy(unknown).Should().Be(unknown);
    }

    [Fact]
    public void ToSqlCase_emits_the_same_mapping_as_FromLegacy()
    {
        var sql = TransactionStatusRemap.ToSqlCase("\"Status\"");

        sql.Should().Be(
            "CASE \"Status\" " +
            $"WHEN 1 THEN {(int)TransactionStatus.Confirmed} " +
            $"WHEN 2 THEN {(int)TransactionStatus.Scheduled} " +
            $"WHEN 3 THEN {(int)TransactionStatus.Scheduled} " +
            "ELSE \"Status\" END");
    }
}
