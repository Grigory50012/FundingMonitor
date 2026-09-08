using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Contracts;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiContractSnapshotTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task OpenApi_FundingRateNumbers_RemainStrictJsonNumbers()
    {
        await using var developmentFactory =
            fixture.Factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = developmentFactory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var properties = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("FundingRateDto").GetProperty("properties");
        foreach (var property in new[] { "markPrice", "fundingRate", "apr" })
            Assert.Equal("number", properties.GetProperty(property).GetProperty("type").GetString());
        using var scalar = await client.GetAsync("/scalar", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
    }

    [Fact]
    public async Task EveryPublishedRoute_IsMapped()
    {
        await using var developmentFactory =
            fixture.Factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = developmentFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        foreach (var path in ExpectedRoutes.Paths)
        {
            using var response = await client.GetAsync(
                path,
                TestContext.Current.CancellationToken);

            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
    }
}
