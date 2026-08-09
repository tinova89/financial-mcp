using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Single handler for CreateTransactionCommand — orchestrates persistence of the new
/// transaction. Calculation rules (installments, billing cycle) don't apply here:
/// each row already represents a concrete transaction (see CLAUDE.md > Mediator Pattern).
/// </summary>
public sealed class CreateTransactionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = new Transacao
        {
            Origem = Enum.Parse<OrigemTransacao>(request.Source),
            Tipo = Enum.Parse<TipoTransacao>(request.Type),
            Status = Enum.Parse<StatusTransacao>(request.Status),
            Descricao = request.Description,
            Valor = request.Amount,
            CategoriaBruta = request.RawCategory,
            DataPrevista = request.ExpectedDate,
            DataEfetiva = request.ActualDate,
            DataConciliado = request.ReconciledDate,
            VencimentoFatura = request.InvoiceDueDate,
            Repeticao = request.Recurrence is null ? TipoRepeticao.Nenhuma : Enum.Parse<TipoRepeticao>(request.Recurrence),
            ParcelaAtual = request.CurrentInstallment,
            ParcelaTotal = request.TotalInstallments,
            ContaId = request.AccountId,
            CartaoId = request.CardId
        };

        db.Transactions.Add(transaction);

        // Final SaveChangesAsync is done by TransactionBehavior (commits the database transaction).

        return new TransactionDto(
            transaction.Id,
            transaction.Origem.ToString(),
            transaction.Tipo.ToString(),
            transaction.Status.ToString(),
            transaction.Descricao,
            transaction.Valor,
            transaction.CategoriaBruta,
            transaction.DataPrevista,
            transaction.DataEfetiva,
            transaction.DataConciliado,
            transaction.VencimentoFatura,
            transaction.Repeticao.ToString(),
            transaction.ParcelaAtual,
            transaction.ParcelaTotal,
            transaction.ContaId,
            transaction.CartaoId);
    }
}
