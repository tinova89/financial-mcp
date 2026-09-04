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
            .Include(x => x.Category).ThenInclude(c => c.ParentCategory)
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken);

        if (t is null)
        {
            throw new NotFoundException(nameof(Transaction), request.TransactionId);
        }

        return new TransactionDto(
            t.Id, t.Type.ToString(), t.Status.ToString(), t.Description, t.Amount,
            t.Category.FullName, t.ExpectedDate, t.ActualDate, t.ConfirmationDate, t.InvoiceDueDate,
            t.Recurrence.ToString(), t.CurrentInstallment, t.TotalInstallments, t.AccountId,
            t.NeedsConfirmation(DateOnly.FromDateTime(DateTime.UtcNow)));
    }
}
