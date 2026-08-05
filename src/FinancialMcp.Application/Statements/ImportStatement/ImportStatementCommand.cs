using FinancialMcp.Application.Common.Behaviors;
using MediatR;

namespace FinancialMcp.Application.Statements.ImportStatement;

/// <summary>
/// Importa um novo extrato CSV (CC ou CD) para a base. Corresponde à tool MCP
/// `import_statement`. Formato de origem: separador ";", datas "dd/mm/aaaa",
/// decimal com ponto (ver CLAUDE.md > MCP e Convenções de Código).
/// </summary>
public sealed record ImportStatementCommand(
    string Origem,          // "ContaCorrente" | "CartaoCredito"
    Guid? ContaId,
    Guid? CartaoId,
    string CsvContent) : IRequest<ImportStatementResultDto>, ITransactionalRequest;

public sealed record ImportStatementResultDto(
    int LinhasProcessadas,
    int LinhasImportadas,
    IReadOnlyList<string> Avisos);
