using FinancialMcp.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FinancialMcp.Infrastructure;
using FinancialMcp.Console.CsvTest;

// Generic Host: the console equivalent of WebApplication.CreateBuilder used in
// FinancialMcp.Api (see CLAUDE.md > Architecture). There is no IApplicationBuilder
// here because that interface is specific to the ASP.NET Core HTTP request pipeline
// (app.Use...) — outside a web host, DI is exposed via builder.Services / IHost.Services.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddConsoleServices();
builder.Services.AddInfrastructure(builder.Configuration);

using IHost host = builder.Build();

using (IServiceScope scope = host.Services.CreateScope())
{
    var cvstest = scope.ServiceProvider.GetRequiredService<CsvTestService>();
    cvstest.Test();
}
