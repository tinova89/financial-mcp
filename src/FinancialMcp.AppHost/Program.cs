// FinancialMcp.AppHost / Program.cs
// Local orchestration via .NET Aspire: provisions Postgres, injects the connection
// string into the API via service discovery, and enables the Aspire dashboard.
// See CLAUDE.md > Architecture > Aspire / Persistence (Postgres).

using Aspire.Hosting.Docker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = DistributedApplication.CreateBuilder(args);

// Machine-specific overrides (optional). Loaded after appsettings.json and appsettings.{Environment}.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

string dbname = "financial-db"; // Production
// Check for a custom environment name
if (builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("devlocal"))
{
    dbname = "dev-financial-db";
}
else if (builder.Environment.IsStaging())
{
    dbname = "staging-financial-db";
}

builder.AddDockerComposeEnvironment("env")
    .ConfigureEnvFile(env =>
    {
        //env["FINANCIALMCP_API_IMAGE"] = new CapturedEnvironmentVariable
        //{
        //    Name = "FINANCIALMCP_API_IMAGE",
        //    DefaultValue = $"financialmcp-api:aspire-deploy-{builder.Environment.EnvironmentName}"
        //};
    });

// Ensures `dbname` exists the first time the Postgres container initializes its data
// volume — needed when the stack is brought up via plain `docker compose up` (no
// AppHost .NET process). Under `dotnet run`, AddDatabase(dbname) below already creates
// the database itself once the server is ready, via the AppHost's ResourceReadyEvent;
// this script is a fallback for the docker-compose-only path and, like any Postgres
// /docker-entrypoint-initdb.d script, only runs against a fresh (empty) data volume —
// it won't retroactively create the database on a volume that already exists.
var postgresInitDir = Path.Combine(builder.AppHostDirectory, "postgres-init");
Directory.CreateDirectory(postgresInitDir);
File.WriteAllText(
    Path.Combine(postgresInitDir, "01-create-database.sql"),
    $"""
    SELECT 'CREATE DATABASE "{dbname}"'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '{dbname}')\gexec
    """);

// Explicit secret parameter, no default — AddPostgres would otherwise auto-generate
// one silently. Without a stored/generated value, `aspire deploy` stops and prompts
// for it interactively instead of inventing a password for you. Locally, `dotnet run`
// picks up whatever is already saved under "Parameters:financialmcp-postgres-password"
// in user secrets (set via `dotnet user-secrets set` — see CLAUDE.md > Authentication).
var postgresPassword = builder.AddParameter("financialmcp-postgres-password", "1234");

// Postgres resource — resource name "financialmcp-postgres", database "financialmcp-db".
// In production this resource is replaced by the real connection string via
// environment/secret configuration (never hardcoded here).
var postgres = builder
    .AddPostgres("financialmcp-postgres", password: postgresPassword)
    .WithDataVolume(isReadOnly: false)
    .WithInitFiles(postgresInitDir);

var financialDb = postgres.AddDatabase(dbname);

// pgAdmin, declared as a plain container instead of via WithPgAdmin(), because that
// helper marks its resource ExcludeFromManifest — it never shows up in the generated
// docker-compose.yaml. This version does, so `docker compose up` also gets a pgAdmin UI.
var pgAdminEmail = builder.AddParameter("pgadmin-email", value: "admin@gmail.com", publishValueAsDefault: true);
// No default on purpose (secret) — set locally via:
//   dotnet user-secrets set "Parameters:pgadmin-password" "<value>" --project src/FinancialMcp.AppHost
var pgAdminPassword = builder.AddParameter("pgadmin-password", secret: true);

// Pre-registers the Postgres connection in pgAdmin's UI via servers.json — pgAdmin only
// reads this file the first time its data volume is initialized (see pgAdmin docs on
// Import/Export Servers), so it won't retroactively appear if the volume already exists.
// Regenerated on every AppHost start so host/db always match `postgres`/`dbname` above.
// The password is intentionally left out — pgAdmin prompts for it on first connect.
var pgAdminServersJsonPath = Path.Combine(builder.AppHostDirectory, "pgadmin", "servers.json");
Directory.CreateDirectory(Path.GetDirectoryName(pgAdminServersJsonPath)!);
File.WriteAllText(pgAdminServersJsonPath, JsonSerializer.Serialize(new
{
    Servers = new Dictionary<string, object>
    {
        ["1"] = new
        {
            Name = postgres.Resource.Name,
            Group = "Servers",
            Host = postgres.Resource.Name,
            Port = 5432,
            MaintenanceDB = dbname,
            Username = "postgres",
            SSLMode = "prefer"
        }
    }
}, new JsonSerializerOptions { WriteIndented = true }));

var pgAdmin = builder
    .AddContainer("pgadmin", "dpage/pgadmin4")
    .WithHttpEndpoint(targetPort: 80, name: "http")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", pgAdminEmail)
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", pgAdminPassword)
    .WithReference(financialDb)
    .WithBindMount(pgAdminServersJsonPath, "/pgadmin4/servers.json", isReadOnly: true)
    .WithVolume("financialmcp-pgadmin-data", "/var/lib/pgadmin")
    .WithHttpHealthCheck("/browser")
    .WithExternalHttpEndpoints()
    .WaitFor(financialDb);

// API: ASP.NET Core Web API + MCP Server host, referencing Postgres via
// service discovery (never a hardcoded URL/connection string in the API project).
var api = builder
    .AddProject<Projects.FinancialMcp_Api>("financialmcp-api")
    .WithReference(financialDb)
    .WaitFor(financialDb)
    .WithExternalHttpEndpoints();

// Example React frontend as an npm resource, if/when it exists at /src/financialmcp-web.
// var web = builder.AddNpmApp("financialmcp-web", "../../src/financialmcp-web")
//     .WithReference(api)
//     .WaitFor(api)
//     .WithHttpEndpoint(env: "PORT")
//     .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
