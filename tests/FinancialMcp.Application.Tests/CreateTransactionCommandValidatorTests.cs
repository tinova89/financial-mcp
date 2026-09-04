using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Transactions.CreateTransaction;
using FinancialMcp.Domain.Entities;
using FinancialMcp.Domain.Enums;
using FluentValidation.TestHelper;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #14 — every free-text field on <c>transactions</c> is capped at 256 characters,
/// enforced by FluentValidation on write.
/// </summary>
public class CreateTransactionCommandValidatorTests
{
    private const int Max = Transaction.FreeTextMaxLength;

    private static CreateTransactionCommandValidator BuildValidator()
    {
        // Build the mock DbSet before touching the substitute — BuildMockDbSet() configures
        // its own NSubstitute internally and would otherwise clobber the pending Returns() call.
        var accounts = new List<Account>().BuildMockDbSet();
        var db = Substitute.For<IApplicationDbContext>();
        db.Accounts.Returns(accounts);
        return new CreateTransactionCommandValidator(db);
    }

    private static CreateTransactionCommand ValidCommand(string? description = null, string? rawCategory = null) => new(
        Type: TransactionType.Expense,
        Status: TransactionStatus.Scheduled,
        Description: description ?? "Compra no mercado",
        Amount: -42.00m,
        RawCategory: rawCategory ?? "Mercado",
        ExpectedDate: new DateOnly(2026, 9, 2),
        ActualDate: null,
        ConfirmationDate: null,
        InvoiceDueDate: null,
        Recurrence: RecurrenceType.None,
        CurrentInstallment: null,
        TotalInstallments: null,
        AccountId: Guid.NewGuid());

    [Fact]
    public async Task Accepts_free_text_fields_exactly_at_the_limit()
    {
        var result = await BuildValidator().TestValidateAsync(
            ValidCommand(description: new string('d', Max), rawCategory: new string('c', Max)));

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
        result.ShouldNotHaveValidationErrorFor(x => x.RawCategory);
    }

    [Fact]
    public async Task Rejects_a_description_longer_than_256_characters()
    {
        var result = await BuildValidator().TestValidateAsync(ValidCommand(description: new string('d', Max + 1)));

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Rejects_a_raw_category_longer_than_256_characters()
    {
        var result = await BuildValidator().TestValidateAsync(ValidCommand(rawCategory: new string('c', Max + 1)));

        result.ShouldHaveValidationErrorFor(x => x.RawCategory);
    }

    [Fact]
    public async Task Accepts_a_fully_valid_command()
    {
        var result = await BuildValidator().TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
