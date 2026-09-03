using FluentValidation;

namespace FinancialMcp.Application.Revisions.ApproveRevision;

/// <summary>
/// Mirrors <c>DeleteTransactionCommandValidator</c>: the approval is a one-way move and
/// requires an explicit <c>Confirm = true</c>. The <c>approval</c> scope is <b>not</b>
/// checked here — that is enforced at the MCP tool level (403) before the request reaches
/// MediatR (see CLAUDE.md &gt; Authentication (Custom JWT) &gt; Scopes).
/// </summary>
public sealed class ApproveRevisionCommandValidator : AbstractValidator<ApproveRevisionCommand>
{
    public ApproveRevisionCommandValidator()
    {
        RuleFor(x => x.RevisionId).NotEmpty();

        RuleFor(x => x.Confirm)
            .Equal(true)
            .WithMessage("Operação irreversível: é necessário confirmar explicitamente (Confirm = true) antes de aprovar a revisão.");
    }
}
