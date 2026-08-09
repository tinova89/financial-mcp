using System.ComponentModel;
using FinancialMcp.Application.BudgetGoals.GetBudgetStatus;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class BudgetTools(IMediator mediator)
{
    [McpServerTool(Name = "get_budget_status"), Description(
        "Calculates Gasto_Real, Saldo_Meta and % Utilizado by category/month, per the " +
        "registered budget goals. Applies the rules from CLAUDE.md > Business Rules > " +
        "Budget goals (only Status=Conciliado and Tipo=Despesa).")]
    public Task<IReadOnlyList<BudgetStatusDto>> GetBudgetStatusAsync(
        int year, int month, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetBudgetStatusQuery(year, month), cancellationToken);
}
