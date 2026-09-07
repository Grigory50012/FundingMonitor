using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Contracts;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiContractSnapshotTests(PostgreSqlFixture fixture)
{
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