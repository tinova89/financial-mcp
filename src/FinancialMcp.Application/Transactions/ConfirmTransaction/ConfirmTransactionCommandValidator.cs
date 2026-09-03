using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Transactions.ConfirmTransaction;

/// <summary>
/// Card #16 — <c>confirm_transaction</c> only promotes a transaction that is currently
/// <see cref="TransactionStatus.Scheduled"/> to <see cref="TransactionStatus.Confirmed"/>.
/// A <c>Revision</c> row or an already-<c>Confirmed</c> row is rejected here, before the
/// handler runs. An unknown id passes this rule on purpose, so
/// <see cref="ConfirmTransactionCommandHandler"/> can raise the canonical 404 instead.
/// </summary>
public sealed class ConfirmTransactionCommandValidator : AbstractValidator<ConfirmTransactionCommand>
{
    public ConfirmTransactionCommandValidator(IApplicationDbContext db)
    {
        RuleFor(x => x.TransactionId).NotEmpty();

        RuleFor(x => x.TransactionId)
            .MustAsync(async (id, ct) =>
            {
                var status = await db.Transactions
                    .Where(t => t.Id == id)
                    .Select(t => (TransactionStatus?)t.Status)
                    .FirstOrDefaultAsync(ct);

                // Unknown id → not a validation failure; the handler owns the 404.
                return status is null || status == TransactionStatus.Scheduled;
            })
            .WithMessage("Só é possível confirmar uma transação com status Scheduled (agendada); " +
                "uma transação em Revision ou já Confirmed não pode ser confirmada.");
    }
}
