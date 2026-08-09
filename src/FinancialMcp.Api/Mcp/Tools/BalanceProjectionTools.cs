using System.ComponentModel;
using FinancialMcp.Application.BalanceProjection.GetBalanceProjection;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class BalanceProjectionTools(IMediator mediator)
{
    [McpServerTool(Name = "get_balance_projection"), Description(
        "Gera a projeção de saldo consolidada, aplicando o ciclo de fatura, parcelamento " +
        "e lançamentos fixos do(s) cartão(ões) vinculado(s) à conta informada.")]
    public Task<IReadOnlyList<MonthlyProjectionDto>> GetBalanceProjectionAsync(
        Guid contaId, int mesesAFrente = 6, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetBalanceProjectionQuery(contaId, mesesAFrente), cancellationToken);
}
