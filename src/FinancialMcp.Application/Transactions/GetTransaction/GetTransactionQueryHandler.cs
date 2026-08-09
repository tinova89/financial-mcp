using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.GetTransaction;

public sealed class GetTransactionQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTransactionQuery, TransactionDto>
{
    public async Task<TransactionDto> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
    {
        var t = await db.Transactions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken);

        if (t is null)
        {
            throw new NotFoundException(nameof(Transacao), request.TransactionId);
        }

        return new TransactionDto(
            t.Id, t.Origem.ToString(), t.Tipo.ToString(), t.Status.ToString(), t.Descricao, t.Valor,
            t.CategoriaBruta, t.DataPrevista, t.DataEfetiva, t.DataConciliado, t.VencimentoFatura,
            t.Repeticao.ToString(), t.ParcelaAtual, t.ParcelaTotal, t.ContaId, t.CartaoId);
    }
}
