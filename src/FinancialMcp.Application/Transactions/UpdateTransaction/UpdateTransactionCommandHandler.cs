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
            throw new NotFoundException(nameof(Transaction), request.TransactionId);
        }

        if (request.Status is not null) t.Status = Enum.Parse<TransactionStatus>(request.Status);
        if (request.RawCategory is not null) t.RawCategory = request.RawCategory;
        if (request.Amount is not null) t.Amount = request.Amount.Value;
        if (request.ExpectedDate is not null) t.ExpectedDate = request.ExpectedDate.Value;
        if (request.ActualDate is not null) t.ActualDate = request.ActualDate;
        if (request.ReconciledDate is not null) t.ReconciledDate = request.ReconciledDate;
        if (request.InvoiceDueDate is not null) t.InvoiceDueDate = request.InvoiceDueDate;

        t.UpdatedAt = DateTimeOffset.UtcNow;

        return new TransactionDto(
            t.Id, t.Type.ToString(), t.Status.ToString(), t.Description, t.Amount,
            t.RawCategory, t.ExpectedDate, t.ActualDate, t.ReconciledDate, t.InvoiceDueDate,
            t.Recurrence.ToString(), t.CurrentInstallment, t.TotalInstallments, t.AccountId);
    }
}
