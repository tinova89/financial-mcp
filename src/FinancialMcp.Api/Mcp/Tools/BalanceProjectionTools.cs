using System.ComponentModel;
using FinancialMcp.Application.BalanceProjection.GetBalanceProjection;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class BalanceProjectionTools(IMediator mediator)
{
    [McpServerTool(Name = "get_balance_projection"), Description(
        "Generates the consolidated balance projection, applying the billing cycle, installments " +
        "and fixed entries of the credit card(s) whose bill is paid from the given account.")]
    public Task<IReadOnlyList<MonthlyProjectionDto>> GetBalanceProjectionAsync(
        Guid accountId, int monthsAhead = 6, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetBalanceProjectionQuery(accountId, monthsAhead), cancellationToken);
}
