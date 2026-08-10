using FinancialMcp.Api.Auth;
using FinancialMcp.Api.Common;
using FinancialMcp.Application;
using FinancialMcp.Infrastructure;
using FinancialMcp.Infrastructure.Persistence;
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

await app.RunAsync();

// Exposed so the Aspire AppHost can reference it via AddProject<Projects.FinancialMcp_Api>.
namespace FinancialMcp.Api
{
    public partial class Program;
}
