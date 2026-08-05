using System.ComponentModel;
using FinancialMcp.Application.Statements.ImportStatement;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class StatementTools(IMediator mediator)
{
    [McpServerTool(Name = "import_statement"), Description(
        "Importa um novo extrato CSV (Conta Corrente ou Cartão de Crédito) para a base. " +
        "Formato esperado: separador ';', datas 'dd/mm/aaaa', decimal com ponto.")]
    public Task<ImportStatementResultDto> ImportStatementAsync(
        string origem, string csvContent, Guid? contaId = null, Guid? cartaoId = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ImportStatementCommand(origem, contaId, cartaoId, csvContent), cancellationToken);
}
