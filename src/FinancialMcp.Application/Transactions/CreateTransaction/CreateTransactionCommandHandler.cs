using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using MediatR;

namespace FinancialMcp.Application.Transactions.CreateTransaction;

/// <summary>
/// Handler único para CreateTransactionCommand — orquestra a persistência da nova
/// transação. Regras de cálculo (parcelamento, ciclo de fatura) não se aplicam aqui:
/// cada linha já representa uma transação concreta (ver CLAUDE.md > Padrão Mediator).
/// </summary>
public sealed class CreateTransactionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transacao = new Transacao
        {
            Origem = Enum.Parse<OrigemTransacao>(request.Origem),
            Tipo = Enum.Parse<TipoTransacao>(request.Tipo),
            Status = Enum.Parse<StatusTransacao>(request.Status),
            Descricao = request.Descricao,
            Valor = request.Valor,
            CategoriaBruta = request.CategoriaBruta,
            DataPrevista = request.DataPrevista,
            DataEfetiva = request.DataEfetiva,
            DataConciliado = request.DataConciliado,
            VencimentoFatura = request.VencimentoFatura,
            Repeticao = request.Repeticao is null ? TipoRepeticao.Nenhuma : Enum.Parse<TipoRepeticao>(request.Repeticao),
            ParcelaAtual = request.ParcelaAtual,
            ParcelaTotal = request.ParcelaTotal,
            ContaId = request.ContaId,
            CartaoId = request.CartaoId
        };

        db.Transacoes.Add(transacao);

        // SaveChangesAsync final é feito pelo TransactionBehavior (commit da transação de banco).

        return new TransactionDto(
            transacao.Id,
            transacao.Origem.ToString(),
            transacao.Tipo.ToString(),
            transacao.Status.ToString(),
            transacao.Descricao,
            transacao.Valor,
            transacao.CategoriaBruta,
            transacao.DataPrevista,
            transacao.DataEfetiva,
            transacao.DataConciliado,
            transacao.VencimentoFatura,
            transacao.Repeticao.ToString(),
            transacao.ParcelaAtual,
            transacao.ParcelaTotal,
            transacao.ContaId,
            transacao.CartaoId);
    }
}
