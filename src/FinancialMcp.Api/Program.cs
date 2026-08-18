using FinancialMcp.Api.Auth;
using FinancialMcp.Api.Common;
using FinancialMcp.Application;
using FinancialMcp.Application.Accounts.CreateAccount;
using FinancialMcp.Application.Accounts.DeleteAccount;
using FinancialMcp.Application.Accounts.UpdateAccount;
using FinancialMcp.Application.CreditCards.CreateCreditCard;
using FinancialMcp.Application.CreditCards.DeleteCreditCard;
using FinancialMcp.Application.CreditCards.UpdateCreditCard;
using FinancialMcp.Infrastructure;
using FinancialMcp.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Machine-specific overrides (optional). Loaded after appsettings.json and appsettings.{Environment}.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Shared Aspire defaults: OpenTelemetry, health checks, resilience
// (see CLAUDE.md > Architecture > Aspire).
builder.AddServiceDefaults();

// Postgres via Aspire integration: the connection string for the "financialmcp-db"
// resource is injected automatically by service discovery (never hardcoded here).
builder.AddNpgsqlDbContext<ApplicationDbContext>("financialmcp-db");


// Application layers (see CLAUDE.md > Mediator Pattern > Registration and > Authentication).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
//builder.Services.AddInfrastructureAuth(builder.Configuration);
builder.Services.AddInfrastructurePersistence();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// MCP host over the same process/port as the API, authenticated via JWT bearer
// (see CLAUDE.md > MCP > Auth) and with tools registered by reflection.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapDefaultEndpoints(); // "/health", "/alive" — see ServiceDefaults.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Local machine only: set Hosting:ApplyDatabaseMigrationsOnStartup in appsettings.Local.json.
if (app.Environment.IsDevelopment()
    && app.Configuration.GetValue("Hosting:ApplyDatabaseMigrationsOnStartup", false))
{
    using IServiceScope scope = app.Services.CreateScope();
    ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();
    //if (app.Configuration.GetValue("Hosting:ApplySampleFinancialDataAfterMigrations", false))
    //{
    //    await FinancialSampleDataSeeder.EnsureInitialSampleDataAsync(db);
    //}
}

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapAuthEndpoints();

// MCP endpoint authenticated via JWT bearer — same scheme/pipeline as the REST API.
app.MapMcp("/mcp")
    //.RequireAuthorization()
    ;

#region Checking Accounts API
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// Financial accounts API.
/// </summary>
RouteGroupBuilder? accounts = app.MapGroup("/api/financial/checking-accounts");
accounts.WithTags("CheckingAccounts");

accounts.MapPost(
        "/",
        async (IMediator mediator, [FromBody] CreateAccountCommand command) => await mediator.Send(command))
    .WithName("CreateCheckingAccount")
    .WithDescription("""
        Registers a new financial account (a plain, non-credit-card account — checking,
        investment, wallet, etc.) so that transactions and credit cards can be linked to it.
    
        ## Parameters
        - **bankCode** — COMPE bank code identifying the institution. Supported values:
          `341` (Itaú), `260` (Nubank), `077` (Inter), `307` (Wallet). An unrecognized
          code is rejected by validation before persisting.
        - **displayName** — Friendly name shown to the user (e.g. `"Nubank Corrente"`).
          Required, up to 200 characters.
        - **initialAmount** — Opening balance for the account, expressed in `baseCurrency`.
        - **baseCurrency** — ISO currency code for the account's balance. Supported values:
          `BRL`, `USD`, `BTC`. An unrecognized code is rejected by validation before persisting.
    
        ## Not a parameter
        - **kind** is intentionally absent — it's computed from the entity's own type, not
          a stored/settable field: a plain account created here always reports `"Debit"`;
          only a `CreditCard` (created via `create_credit_card`) reports `"Credit"`.
    
        ## Behavior
        - The account is created with no linked credit cards (`creditCardIds` starts
          empty); link credit cards afterwards via `create_credit_card`'s
          `paymentAccountId` parameter (see `CreditCardTools`).
        - This is a non-destructive write; no confirmation is required.
    
        ## Example
        ```json
        { "bankCode": "260", "displayName": "Nubank Corrente", "initialAmount": 1500.00, "baseCurrency": "BRL" }
        ```
    
        ## Returns
        The created `AccountDto` (id, displayName, bankCode, initialAmount, kind, baseCurrencyCode, creditCardIds).
        """);

accounts.MapPut(
        "/{accountId:Guid}",
        async (Guid accountId, IMediator mediator, [FromBody] UpdateAccountCommand body) =>
            await mediator.Send(new UpdateAccountCommand(
                AccountId: accountId,
                DisplayName: body.DisplayName,
                BankCode: body.BankCode,
                InitialAmount: body.InitialAmount,
                BaseCurrencyCode: body.BaseCurrencyCode
                )))
    .WithName("UpdateCheckingAccount")
    .WithDescription("""
        Changes one or more fields of an existing account. This is a partial patch: only the
        parameters explicitly provided are changed — any parameter left as `null` keeps its
        current stored value.

        ## Parameters
        - **accountId** — `Guid` of the account to update. Required.
        - **displayName** — New friendly name, up to 200 characters. Optional.
        - **bankCode** — New COMPE bank code. Must be one of the supported codes: `341` (Itaú),
          `260` (Nubank), `077` (Inter), `307` (Wallet). Optional; rejected by validation if
          unrecognized.
        - **initialAmount** — New opening balance, expressed in the account's `baseCurrencyCode`.
          Optional.
        - **baseCurrencyCode** — New ISO currency code. Supported values: `BRL`, `USD`, `BTC`.
          Optional; rejected by validation if unrecognized.

        ## Not a parameter
        - **kind** cannot be changed — it's computed from the entity's own type, always
          `"Debit"` for accounts managed by this tool.

        ## Behavior
        - Throws a not-found error if `accountId` doesn't match a registered, non-credit-card account.
        - Non-destructive; no confirmation is required.
        - Existing linked credit cards (`creditCardIds`) are unaffected by this call.

        ## Example
        ```json
        { "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "displayName": "Nubank Corrente 2", "initialAmount": 2000.00 }
        ```

        ## Returns
        The updated `AccountDto` (id, displayName, bankCode, initialAmount, kind, baseCurrencyCode, creditCardIds).
        """);

accounts.MapDelete(
        "/{accountId:Guid}/{confirm:bool}",
        async (Guid accountId, bool confirm, IMediator mediator) =>
            await mediator.Send(new DeleteAccountCommand(accountId, confirm)))
    .WithName("DeleteCheckingAccount")
    .WithDescription("""
        Removes an account via soft delete (sets `IsDeleted`/`DeletedAt`; the row is never
        physically removed and disappears from all default queries afterwards).

        ## Parameters
        - **accountId** — `Guid` of the account to remove. Required.
        - **confirm** — Must be explicitly `true` for the operation to proceed. Required.

        ## DESTRUCTIVE OPERATION
        Always confirm explicitly with the user before calling this tool with `confirm = true`.
        Calling it with `confirm = false` (or omitted) is rejected by validation before any
        data is touched.

        ## Behavior
        - Throws a not-found error if `accountId` doesn't match a registered, non-credit-card
          account (use `delete_credit_card` for credit cards).
        - Does not cascade to linked transactions or credit cards — they keep their
          `accountId`/`paymentAccountId` references; consider that before deleting an
          account still in active use.

        ## Example
        ```json
        { "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "confirm": true }
        ```

        ## Returns
        A confirmation message string ("Conta removida (soft delete).").
        """);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#endregion

#region Credit Cards API
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// Credit cards API.
/// </summary>
RouteGroupBuilder? creditCards = app.MapGroup("/api/financial/credit-cards");
creditCards.WithTags("CreditCards");

creditCards.MapPost(
        "/",
        async (IMediator mediator, [FromBody] CreateCreditCardCommand command) => await mediator.Send(command))
    .WithName("CreateCreditCard")
    .WithDescription("""
        Registers a new credit card, linked to the Account debited when its bill is paid.

        ## Parameters
        - **bankCode** — COMPE bank code identifying the institution. Supported values:
          `341` (Itaú), `260` (Nubank), `077` (Inter), `307` (Wallet). An unrecognized
          code is rejected by validation before persisting.
        - **displayName** — Friendly name shown to the user (e.g. `"Nubank Mastercard"`).
          Required, up to 200 characters.
        - **initialAmount** — Opening balance for the card, expressed in `baseCurrencyCode`.
        - **baseCurrencyCode** — ISO currency code. Supported values: `BRL`, `USD`, `BTC`.
          An unrecognized code is rejected by validation before persisting.
        - **closingDay** — Day of the month the bill closes. Must be between 1 and 28
          (capped at 28 so the value is valid in every month, avoiding month-end edge cases).
        - **dueDay** — Day of the month the bill is due. Same 1-28 range.
        - **paymentAccountId** — `Guid` of the *other* account debited to pay this card's
          bill. Must reference an existing account that is **not itself a credit card**
          (rejected with a not-found error otherwise) — this prevents a card being
          configured to pay itself via another card.

        ## Not a parameter
        - **kind** is intentionally absent — every credit card's `Kind` is always forced
          to `"Credit"` by the handler, never settable here.

        ## Behavior
        - Non-destructive write; no confirmation is required.

        ## Example
        ```json
        { "bankCode": "260", "displayName": "Nubank Mastercard", "initialAmount": 0,
          "baseCurrencyCode": "BRL", "closingDay": 5, "dueDay": 12,
          "paymentAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
        ```

        ## Returns
        The created `CreditCardDto` (id, displayName, bankCode, initialAmount, kind,
        baseCurrencyCode, closingDay, dueDay, paymentAccountId).
        """);

creditCards.MapPut(
        "/{creditCardId:Guid}",
        async (Guid creditCardId, IMediator mediator, [FromBody] UpdateCreditCardCommand body) =>
            await mediator.Send(new UpdateCreditCardCommand(
                CreditCardId: creditCardId,
                DisplayName: body.DisplayName,
                BankCode: body.BankCode,
                InitialAmount: body.InitialAmount,
                BaseCurrencyCode: body.BaseCurrencyCode,
                ClosingDay: body.ClosingDay,
                DueDay: body.DueDay,
                PaymentAccountId: body.PaymentAccountId
                )))
    .WithName("UpdateCreditCard")
    .WithDescription("""
        Changes one or more fields of an existing credit card. This is a partial patch:
        only the parameters explicitly provided are changed — any parameter left as
        `null` keeps its current stored value.

        ## Parameters
        - **creditCardId** — `Guid` of the credit card to update. Required.
        - **displayName** — New friendly name, up to 200 characters. Optional.
        - **bankCode** — New COMPE bank code (see `create_credit_card` for supported
          values). Optional; rejected by validation if unrecognized.
        - **initialAmount** — New opening balance. Optional.
        - **baseCurrencyCode** — New ISO currency code (`BRL`, `USD`, `BTC`). Optional;
          rejected by validation if unrecognized.
        - **closingDay** — New closing day (1-28). Optional.
        - **dueDay** — New due day (1-28). Optional.
        - **paymentAccountId** — New payment account. Optional; must reference an
          existing, non-credit-card account (rejected with a not-found error otherwise).

        ## Not a parameter
        - **kind** cannot be changed — it's always `"Credit"`.

        ## Behavior
        - Throws a not-found error if `creditCardId` doesn't match a registered credit card.
        - Non-destructive; no confirmation is required.

        ## Example
        ```json
        { "creditCardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "closingDay": 10, "dueDay": 17 }
        ```

        ## Returns
        The updated `CreditCardDto` (id, displayName, bankCode, initialAmount, kind,
        baseCurrencyCode, closingDay, dueDay, paymentAccountId).
        """);

creditCards.MapDelete(
        "/{creditCardId:Guid}/{confirm:bool}",
        async (Guid creditCardId, bool confirm, IMediator mediator) =>
        {
            await mediator.Send(new DeleteCreditCardCommand(creditCardId, confirm));
            return "Cartão de crédito removido (soft delete).";
        })
    .WithName("DeleteCreditCard")
    .WithDescription("""
        Removes a credit card via soft delete (sets `IsDeleted`/`DeletedAt`; the row is
        never physically removed and disappears from all default queries afterwards).

        ## Parameters
        - **creditCardId** — `Guid` of the credit card to remove. Required.
        - **confirm** — Must be explicitly `true` for the operation to proceed. Required.

        ## DESTRUCTIVE OPERATION
        Always confirm explicitly with the user before calling this tool with
        `confirm = true`. Calling it with `confirm = false` (or omitted) is rejected by
        validation before any data is touched.

        ## Behavior
        - Throws a not-found error if `creditCardId` doesn't match a registered credit card.
        - Does not cascade to linked transactions — they keep their `accountId` reference
          (a credit card's transactions use its own id as `accountId`); consider that before
          deleting a credit card still in active use.

        ## Example
        ```json
        { "creditCardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "confirm": true }
        ```

        ## Returns
        A confirmation message string ("Cartão de crédito removido (soft delete).").
        """);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#endregion

await app.RunAsync();

// Exposed so the Aspire AppHost can reference it via AddProject<Projects.FinancialMcp_Api>.
namespace FinancialMcp.Api
{
    public partial class Program;
}
