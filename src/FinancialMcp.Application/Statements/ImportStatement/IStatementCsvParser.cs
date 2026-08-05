using FinancialMcp.Domain.Entities;

namespace FinancialMcp.Application.Statements.ImportStatement;

/// <summary>
/// Parser do formato de extrato (separador ";", datas dd/mm/aaaa, decimal com
/// ponto). Implementado em FinancialMcp.Infrastructure (usa CsvHelper) e injetado
/// aqui via abstração, para manter a Application livre de dependência de I/O.
/// </summary>
public interface IStatementCsvParser
{
    IReadOnlyList<Transacao> Parse(string csvContent, string origem, Guid? contaId, Guid? cartaoId, out IReadOnlyList<string> avisos);
}
