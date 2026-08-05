namespace FinancialMcp.Application.Common.Exceptions;

public sealed class NotFoundException(string entityName, object key)
    : Exception($"Entidade \"{entityName}\" ({key}) não encontrada.");
