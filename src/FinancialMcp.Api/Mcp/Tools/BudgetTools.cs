using System.ComponentModel;
using FinancialMcp.Application.BudgetGoals.GetBudgetStatus;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class BudgetTools(IMediator mediator)
{
    [McpServerTool(Name = "get_budget_status"), Description(
        "Calcula Gasto_Real, Saldo_Meta e % Utilizado por categoria/mês, conforme metas " +
        "de orçamento cadastradas. Aplica as regras de CLAUDE.md > Regras de Negócio > " +
        "Metas de orçamento (apenas Status=Conciliado e Tipo=Despesa).")]
    public Task<IReadOnlyList<BudgetStatusDto>> GetBudgetStatusAsync(
        int ano, int mes, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetBudgetStatusQuery(ano, mes), cancellationToken);
}
