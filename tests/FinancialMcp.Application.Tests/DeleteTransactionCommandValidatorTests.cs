using FinancialMcp.Application.Transactions.DeleteTransaction;
using FluentValidation.TestHelper;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `delete_transaction` is a destructive operation: <c>Confirm</c> must be
/// explicitly <c>true</c>. This guard lives entirely in the validator (enforced by
/// <c>ValidationBehavior</c> before the handler runs) — see
/// <see cref="DeleteTransactionCommandHandlerTests"/> for the handler's own behavior.
/// </summary>
public class DeleteTransactionCommandValidatorTests
{
    private static readonly DeleteTransactionCommandValidator Validator = new();

    [Fact]
    public async Task Rejects_when_confirm_is_false()
    {
        var result = await Validator.TestValidateAsync(new DeleteTransactionCommand(Guid.NewGuid(), Confirm: false));

        result.ShouldHaveValidationErrorFor(x => x.Confirm);
    }

    [Fact]
    public async Task Rejects_when_confirm_is_left_at_its_default()
    {
        var result = await Validator.TestValidateAsync(new DeleteTransactionCommand(Guid.NewGuid(), default));

        result.ShouldHaveValidationErrorFor(x => x.Confirm);
    }

    [Fact]
    public async Task Accepts_when_confirm_is_true()
    {
        var result = await Validator.TestValidateAsync(new DeleteTransactionCommand(Guid.NewGuid(), Confirm: true));

        result.ShouldNotHaveValidationErrorFor(x => x.Confirm);
    }

    [Fact]
    public async Task Rejects_an_empty_transaction_id()
    {
        var result = await Validator.TestValidateAsync(new DeleteTransactionCommand(Guid.Empty, Confirm: true));

        result.ShouldHaveValidationErrorFor(x => x.TransactionId);
    }
}
