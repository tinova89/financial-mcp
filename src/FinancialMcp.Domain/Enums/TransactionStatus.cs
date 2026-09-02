namespace FinancialMcp.Domain.Enums;

/// <summary>
/// Transaction lifecycle status (see CLAUDE.md > Business Rules).
/// <list type="bullet">
///   <item><description><c>Revision</c> — submitted for review, not yet accepted onto the ledger.</description></item>
///   <item><description><c>Scheduled</c> — accepted and planned; still expected to change until confirmed.</description></item>
///   <item><description><c>Confirmed</c> — actually happened; the only status that feeds <c>get_budget_status</c>' Gasto_Real.</description></item>
/// </list>
/// Legacy values are remapped by the <c>Card14TransactionStatuses</c> migration:
/// <c>Agendado</c>/<c>Nconciliado</c> → <see cref="Scheduled"/>, <c>Conciliado</c> → <see cref="Confirmed"/>
/// (see <see cref="TransactionStatusRemap"/>).
/// </summary>
public enum TransactionStatus
{
    Revision = 1,
    Scheduled = 2,
    Confirmed = 3
}
