using FinancialMcp.Application.Common.Interfaces;
using FinancialMcp.Application.Tests.Support;
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

    private static CreateTransactionCommandValidator BuildValidator(params Account[] seededAccounts)
    {
        // Build the mock DbSet before touching the substitute — BuildMockDbSet() configures
        // its own NSubstitute internally and would otherwise clobber the pending Returns() call.
        var accounts = seededAccounts.ToList().BuildMockDbSet();
        var db = Substitute.For<IApplicationDbContext>();
        db.Accounts.Returns(accounts);
        return new CreateTransactionCommandValidator(db);
    }

    private static CreateTransactionCommand ValidCommand(
        string? description = null,
        string? rawCategory = null,
        TransactionStatus status = TransactionStatus.Scheduled,
        DateOnly? confirmationDate = null,
        DateOnly? invoiceDueDate = null,
        RecurrenceType recurrence = RecurrenceType.None,
        int? currentInstallment = null,
        int? totalInstallments = null,
        Guid? accountId = null) => new(
        Type: TransactionType.Expense,
        Status: status,
        Description: description ?? "Compra no mercado",
        Amount: -42.00m,
        RawCategory: rawCategory ?? "Mercado",
        ExpectedDate: new DateOnly(2026, 9, 2),
        ActualDate: null,
        ConfirmationDate: confirmationDate,
        InvoiceDueDate: invoiceDueDate,
        Recurrence: recurrence,
        CurrentInstallment: currentInstallment,
        TotalInstallments: totalInstallments,
        AccountId: accountId ?? Guid.NewGuid());

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

    /// <summary>
    /// Card #20 — per-statement-type required-field rules (invoiceDueDate for credit card,
    /// installment fields for Recurrence = Installment, confirmationDate for Status =
    /// Confirmed), not covered by the Card #14 free-text-cap cases above.
    /// </summary>
    [Fact]
    public async Task Requires_confirmation_date_when_status_is_confirmed()
    {
        var result = await BuildValidator().TestValidateAsync(
            ValidCommand(status: TransactionStatus.Confirmed, confirmationDate: null));

        result.ShouldHaveValidationErrorFor(x => x.ConfirmationDate);
    }

    [Fact]
    public async Task Accepts_a_confirmed_status_with_a_confirmation_date()
    {
        var result = await BuildValidator().TestValidateAsync(
            ValidCommand(status: TransactionStatus.Confirmed, confirmationDate: new DateOnly(2026, 9, 2)));

        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmationDate);
    }

    [Fact]
    public async Task Requires_invoice_due_date_when_the_account_id_is_a_credit_card()
    {
        var checkingAccount = RevisionSeed.NewAccount();
        var creditCard = RevisionSeed.NewCreditCard("Nubank Card", checkingAccount);

        var result = await BuildValidator(checkingAccount, creditCard).TestValidateAsync(
            ValidCommand(accountId: creditCard.Id, invoiceDueDate: null));

        result.ShouldHaveValidationErrorFor(x => x.InvoiceDueDate);
    }

    [Fact]
    public async Task Does_not_require_invoice_due_date_for_a_checking_account()
    {
        var checkingAccount = RevisionSeed.NewAccount();

        var result = await BuildValidator(checkingAccount).TestValidateAsync(
            ValidCommand(accountId: checkingAccount.Id, invoiceDueDate: null));

        result.ShouldNotHaveValidationErrorFor(x => x.InvoiceDueDate);
    }

    [Fact]
    public async Task Requires_installment_fields_when_recurrence_is_installment_for_a_credit_card_account()
    {
        var checkingAccount = RevisionSeed.NewAccount();
        var creditCard = RevisionSeed.NewCreditCard("Nubank Card", checkingAccount);

        var result = await BuildValidator(checkingAccount, creditCard).TestValidateAsync(ValidCommand(
            accountId: creditCard.Id,
            invoiceDueDate: new DateOnly(2026, 9, 10),
            recurrence: RecurrenceType.Installment,
            currentInstallment: null,
            totalInstallments: null));

        result.ShouldHaveValidationErrorFor(x => x.CurrentInstallment);
        result.ShouldHaveValidationErrorFor(x => x.TotalInstallments);
    }

    [Fact]
    public async Task Rejects_a_total_installments_lower_than_the_current_installment()
    {
        var checkingAccount = RevisionSeed.NewAccount();
        var creditCard = RevisionSeed.NewCreditCard("Nubank Card", checkingAccount);

        var result = await BuildValidator(checkingAccount, creditCard).TestValidateAsync(ValidCommand(
            accountId: creditCard.Id,
            invoiceDueDate: new DateOnly(2026, 9, 10),
            recurrence: RecurrenceType.Installment,
            currentInstallment: 5,
            totalInstallments: 3));

        result.ShouldHaveValidationErrorFor(x => x.TotalInstallments);
    }

    [Fact]
    public async Task Accepts_valid_installment_fields_for_a_credit_card_account()
    {
        var checkingAccount = RevisionSeed.NewAccount();
        var creditCard = RevisionSeed.NewCreditCard("Nubank Card", checkingAccount);

        var result = await BuildValidator(checkingAccount, creditCard).TestValidateAsync(ValidCommand(
            accountId: creditCard.Id,
            invoiceDueDate: new DateOnly(2026, 9, 10),
            recurrence: RecurrenceType.Installment,
            currentInstallment: 2,
            totalInstallments: 3));

        result.ShouldNotHaveValidationErrorFor(x => x.CurrentInstallment);
        result.ShouldNotHaveValidationErrorFor(x => x.TotalInstallments);
    }

    [Fact]
    public async Task Does_not_require_installment_fields_for_a_checking_account_even_when_recurrence_is_installment()
    {
        // Credit-card-only rules are gated on the referenced account's actual Kind — a
        // checking-account row is never checked against them, regardless of Recurrence.
        var checkingAccount = RevisionSeed.NewAccount();

        var result = await BuildValidator(checkingAccount).TestValidateAsync(ValidCommand(
            accountId: checkingAccount.Id,
            recurrence: RecurrenceType.Installment,
            currentInstallment: null,
            totalInstallments: null));

        result.ShouldNotHaveValidationErrorFor(x => x.CurrentInstallment);
        result.ShouldNotHaveValidationErrorFor(x => x.TotalInstallments);
    }
}
