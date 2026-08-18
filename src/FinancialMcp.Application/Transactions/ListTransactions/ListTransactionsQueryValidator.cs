using FluentValidation;
using FinancialMcp.Domain.Enums;

namespace FinancialMcp.Application.Transactions.ListTransactions;

public sealed class ListTransactionsQueryValidator : AbstractValidator<ListTransactionsQuery>
{
    public ListTransactionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        RuleFor(x => x.Type)
            .Must(value => Enum.TryParse<TransactionType>(value, ignoreCase: true, out _))
            .When(x => x.Type is not null)
            .WithMessage($"Type deve ser um dos valores: {string.Join(", ", Enum.GetNames<TransactionType>())}.");

        RuleFor(x => x.Status)
            .Must(value => Enum.TryParse<TransactionStatus>(value, ignoreCase: true, out _))
            .When(x => x.Status is not null)
            .WithMessage($"Status deve ser um dos valores: {string.Join(", ", Enum.GetNames<TransactionStatus>())}.");

        RuleFor(x => x.PeriodEnd)
            .GreaterThanOrEqualTo(x => x.PeriodStart)
            .WithMessage("PeriodoFim deve ser maior ou igual a PeriodoInicio.");

        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month is not null && x.Month > 0);
    }
}
