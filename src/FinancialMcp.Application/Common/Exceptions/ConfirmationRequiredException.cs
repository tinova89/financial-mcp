namespace FinancialMcp.Application.Common.Exceptions;

/// <summary>
/// Thrown when a destructive operation (e.g. DeleteTransactionCommand) is
/// executed without the explicit confirmation field set (see CLAUDE.md >
/// What Claude Should Avoid > "Never run delete_transaction without confirmation").
/// </summary>
public sealed class ConfirmationRequiredException(string operationName)
    : Exception($"A operação \"{operationName}\" é destrutiva e requer confirmação explícita (Confirm = true).");
