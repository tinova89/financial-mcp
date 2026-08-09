using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.UpdateTransaction;

public sealed class UpdateTransactionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var t = await db.Transactions
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken);

        if (t is null)
        {
            throw new NotFoundException(nameof(Transacao), request.TransactionId);
        }

        if (request.Status is not null) t.Status = Enum.Parse<StatusTransacao>(request.Status);
        if (request.RawCategory is not null) t.CategoriaBruta = request.RawCategory;
        if (request.Amount is not null) t.Valor = request.Amount.Value;
        if (request.ExpectedDate is not null) t.DataPrevista = request.ExpectedDate.Value;
        if (request.ActualDate is not null) t.DataEfetiva = request.ActualDate;
        if (request.ReconciledDate is not null) t.DataConciliado = request.ReconciledDate;
        if (request.InvoiceDueDate is not null) t.VencimentoFatura = request.InvoiceDueDate;

        t.UpdatedAt = DateTimeOffset.UtcNow;

        return new TransactionDto(
            t.Id, t.Origem.ToString(), t.Tipo.ToString(), t.Status.ToString(), t.Descricao, t.Valor,
            t.CategoriaBruta, t.DataPrevista, t.DataEfetiva, t.DataConciliado, t.VencimentoFatura,
            t.Repeticao.ToString(), t.ParcelaAtual, t.ParcelaTotal, t.ContaId, t.CartaoId);
    }
}
