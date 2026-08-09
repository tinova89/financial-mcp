using System.Net;
using FinancialMcp.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FinancialMcp.Api.Tests;

/// <summary>
/// Basic integration test via TestServer/WebApplicationFactory (see CLAUDE.md
/// > Testing Guidelines > "TestServer + a real MCP client for integration tests
/// of the exposed tools"). Serves as a skeleton for the MCP tools' integration
/// tests (list_transactions, create_transaction, etc.).
/// </summary>
public class HealthCheckTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Alive_endpoint_should_respond_ok()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/alive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
