namespace FinancialMcp.Application.Common.Exceptions;

/// <summary>
/// Lançada quando uma operação destrutiva (ex.: DeleteTransactionCommand) é
/// executada sem o campo de confirmação explícita setado (ver CLAUDE.md >
/// O que o Claude Deve Evitar > "Não executar delete_transaction sem confirmação").
/// </summary>
public sealed class ConfirmationRequiredException(string operationName)
    : Exception($"A operação \"{operationName}\" é destrutiva e requer confirmação explícita (Confirm = true).");
