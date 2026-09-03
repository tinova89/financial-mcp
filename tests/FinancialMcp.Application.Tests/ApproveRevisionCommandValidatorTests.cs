using FinancialMcp.Application.Revisions.ApproveRevision;
using FluentValidation.TestHelper;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #15 — <c>approve_revision</c> requires an explicit <c>Confirm = true</c>
/// (same one-way-action guard as <c>delete_transaction</c>).
/// </summary>
public class ApproveRevisionCommandValidatorTests
{
    private static readonly ApproveRevisionCommandValidator Validator = new();

    [Fact]
    public async Task Rejects_when_confirm_is_false()
    {
        var result = await Validator.TestValidateAsync(new ApproveRevisionCommand(Guid.NewGuid(), Confirm: false));

        result.ShouldHaveValidationErrorFor(x => x.Confirm);
    }

    [Fact]
    public async Task Rejects_when_revision_id_is_empty()
    {
        var result = await Validator.TestValidateAsync(new ApproveRevisionCommand(Guid.Empty, Confirm: true));

        result.ShouldHaveValidationErrorFor(x => x.RevisionId);
    }

    [Fact]
    public async Task Accepts_when_confirmed_with_a_revision_id()
    {
        var result = await Validator.TestValidateAsync(new ApproveRevisionCommand(Guid.NewGuid(), Confirm: true));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
