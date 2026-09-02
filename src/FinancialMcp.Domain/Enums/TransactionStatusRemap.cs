namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Single source of truth for the Card #14 legacy → current <see cref="TransactionStatus"/>
/// remap. Consumed both by the <c>Card14TransactionStatuses</c> EF Core migration (to build
/// its <c>UPDATE ... CASE</c> statement) and by its unit tests, so the two can't drift apart.
///
/// Legacy numeric values (pre-Card #14 enum):
/// <list type="bullet">
///   <item><description><c>1</c> = <c>Reconciled</c> ("Conciliado")</description></item>
///   <item><description><c>2</c> = <c>Scheduled</c> ("Agendado")</description></item>
///   <item><description><c>3</c> = <c>Unreconciled</c> ("Nconciliado")</description></item>
/// </list>
/// Mapping: <c>Conciliado</c> → <see cref="TransactionStatus.Confirmed"/> (3),
/// <c>Agendado</c>/<c>Nconciliado</c> → <see cref="TransactionStatus.Scheduled"/> (2).
/// </summary>
public static class TransactionStatusRemap
{
    public const int LegacyReconciled = 1;
    public const int LegacyScheduled = 2;
    public const int LegacyUnreconciled = 3;

    /// <summary>Maps a legacy stored status value to the current <see cref="TransactionStatus"/> value.</summary>
    public static int FromLegacy(int legacyStatus) => legacyStatus switch
    {
        LegacyReconciled => (int)TransactionStatus.Confirmed,
        LegacyScheduled => (int)TransactionStatus.Scheduled,
        LegacyUnreconciled => (int)TransactionStatus.Scheduled,
        _ => legacyStatus
    };

    /// <summary>
    /// SQL <c>CASE</c> body (without the surrounding <c>CASE</c>/<c>END</c>) that applies
    /// <see cref="FromLegacy"/> to a column. <paramref name="column"/> must already be quoted/escaped.
    /// </summary>
    public static string ToSqlCase(string column) =>
        $"CASE {column} " +
        $"WHEN {LegacyReconciled} THEN {(int)TransactionStatus.Confirmed} " +
        $"WHEN {LegacyScheduled} THEN {(int)TransactionStatus.Scheduled} " +
        $"WHEN {LegacyUnreconciled} THEN {(int)TransactionStatus.Scheduled} " +
        $"ELSE {column} END";
}
