using System.ComponentModel;
using FinancialMcp.Application.BudgetGoals.CreateCategoryBudget;
using FinancialMcp.Application.BudgetGoals.GetBudgetStatus;
using FinancialMcp.Domain.Enums;
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
        - Only counts transactions with `Status = Confirmed` and `Type = Expense` — never
          `Revision`, `Scheduled`, `Income`, `Transfer`, or `Payment` (the checking account's
          "Pagamento de cartão" entry is excluded so credit-card spending isn't double-counted).
        - The reference month is `ConfirmationDate` for checking-account transactions and
          `InvoiceDueDate` for credit-card transactions (`Transaction.GetReferenceMonthYear()`,
          driven by `Account.Kind`, not a stored transaction flag).
        - A goal always targets a parent category (never a subcategory) and sums every
          subcategory under it (e.g. a goal on `Moradia` sums all `Moradia/*` rows).
        - Each result includes a per-subcategory breakdown for extra visibility.
        - Per category, the goal in effect for the requested month is picked via
          `BudgetGoal.ResolveEffective`: a `OneTime` goal matches only its own year/month; a
          `Monthly` goal applies from its own year/month onward, until a later `Monthly` row
          for the same category supersedes it. A `OneTime` row wins over a `Monthly` one for
          the exact month it targets.
        - Categories with **no** goal in effect for this year/month are omitted entirely —
          this tool never invents a goal. Use `list_transactions` with category filters to
          see spending on categories that don't have one.
        - `remainingBudget` = `goalAmount - actualSpend` (can go negative when over budget).
        - `percentUsed` = `actualSpend / goalAmount`, or `null` if `goalAmount` is 0.

        ## Example
        ```json
        { "year": 2026, "month": 8 }
        ```

        ## Returns
        A list of `BudgetStatusDto` (rawCategory, goalAmount, actualSpend, remainingBudget,
        percentUsed, subcategoryBreakdown) — one entry per category with a goal in
        that month, omitting categories without one.
        """)]
    public Task<IReadOnlyList<BudgetStatusDto>> GetBudgetStatusAsync(
        int year, int month, CancellationToken cancellationToken = default) =>
        mediator.Send(new GetBudgetStatusQuery(year, month), cancellationToken);

    [McpServerTool(Name = "create_category_budget"), Description(
        """
        Registers a budget goal (goalAmount) for a parent category and calendar month.

        ## Parameters
        - **categoryId** — `Guid` of an existing parent `TransactionCategory`. Required.
          Must reference a parent category, never a subcategory (rejected by validation
          otherwise) — see CLAUDE.md > Category and subcategory.
        - **amount** — Budget amount for the category, in `currencyCode`. Must be greater
          than 0. Required.
        - **currencyCode** — ISO currency code. Supported values: `BRL`, `USD`, `BTC`. An
          unrecognized code is rejected by validation before persisting.
        - **period** — `int` enum (`BudgetPeriodType`), sent as a plain integer, not a
          string. Required. Values: `1 - Monthly`, `2 - OneTime`.
          - `1 - Monthly` applies from `year`/`month` onward, until a later `Monthly` goal
            for the same category (a later `year`/`month`) supersedes it.
          - `2 - OneTime` applies only to the exact `year`/`month` given, with no repetition.
        - **year** / **month** — The goal's own reference month (its `PeriodReference`).
          Required.

        ## Not a parameter here
        - There's no way to discover a `categoryId` through another MCP tool yet
          (`list_categories` currently returns category names, not ids) — until that's
          added, the caller needs the id from another source (e.g. direct data access).

        ## Behavior
        - Non-destructive write; no confirmation is required.
        - Throws a not-found error if `categoryId` doesn't match a registered category.
        - Rejected by validation if `categoryId` references a subcategory, or if a goal
          already exists for the same category/year/month (one goal per category/month).

        ## Example
        ```json
        { "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "amount": 1000.00,
          "currencyCode": "BRL", "period": 2, "year": 2026, "month": 8 }
        ```
        (`period: 2` = OneTime.)

        ## Returns
        The created `BudgetGoalDto` (id, categoryId, categoryName, amount, currencyCode,
        period, year, month).
        """)]
    public Task<BudgetGoalDto> CreateCategoryBudgetAsync(
        Guid categoryId, decimal amount, string currencyCode, BudgetPeriodType period, int year, int month,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new CreateCategoryBudgetCommand(categoryId, amount, currencyCode, period, year, month), cancellationToken);
}
