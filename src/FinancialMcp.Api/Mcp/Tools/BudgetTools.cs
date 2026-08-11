using System.ComponentModel;
using FinancialMcp.Application.BudgetGoals.GetBudgetStatus;
using MediatR;
using ModelContextProtocol.Server;

namespace FinancialMcp.Api.Mcp.Tools;

[McpServerToolType]
public sealed class BudgetTools(IMediator mediator)
{
    [McpServerTool(Name = "get_budget_status"), Description(
        """
        Calculates budget-vs-actual spending for a given month, for every category that has
        a registered budget goal, applying CLAUDE.md > Business Rules > Budget goals.

        ## Parameters
        - **year** — Calendar year to evaluate. Required.
        - **month** — Month to evaluate (1-12). Required.

        Both are required because the calculation is inherently month-scoped — there's no
        sensible "all months" default.

        ## Behavior
        - Read-only.
        - Only counts transactions with `Status = Reconciled` and `Type = Expense` — never
          `Income`, `Transfer`, or `Payment` (the checking account's "Pagamento de cartão"
          entry is excluded so credit-card spending isn't double-counted).
        - The reference month is `ReconciledDate` for checking-account transactions and
          `InvoiceDueDate` for credit-card transactions (`Transaction.GetReferenceMonthYear()`,
          driven by `Account.Kind`, not a stored transaction flag).
        - Aggregation is by parent category by default, or by the exact subcategory if the
          goal was registered with one (e.g. a goal on `Moradia` sums all `Moradia/*` rows;
          a goal on `Moradia/Seguro` only sums that exact subcategory).
        - Each result includes a per-subcategory breakdown for extra visibility, even though
          only the parent-level (or exact) match drives `Gasto_Real`.
        - Categories with **no** registered goal for this year/month are omitted entirely —
          this tool never invents a goal. Use `list_transactions` with category filters to
          see spending on categories that don't have a goal.
        - `remainingBudget` (Saldo_Meta) = `budgetAmount - actualSpent` (can go negative when over budget).
        - `utilizationPercentage` (% Utilizado) = `actualSpent / budgetAmount`, or `null` if `budgetAmount` is 0.

        ## Example
        ```json
        { "year": 2026, "month": 8 }
        ```

        ## Returns
        A list of `BudgetStatusDto` (rawCategory, budgetAmount, actualSpent, remainingBudget,
        utilizationPercentage, subcategoryBreakdown) — one entry per category with a goal in
        that month, omitting categories without one.
        """)]
    public Task<IReadOnlyList<BudgetStatusDto>> GetBudgetStatusAsync(
        int year, int month, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetBudgetStatusQuery(year, month), cancellationToken);
}
