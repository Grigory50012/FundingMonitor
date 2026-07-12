using System.Text.Json;
using FundingMonitor.Api.OpenApi;
using Microsoft.OpenApi;
using Xunit;

namespace FundingMonitor.Api.Tests.OpenApi;

public class OpenApiDocumentWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesValidOpenApiJsonAtomically()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"funding-monitor-{Guid.NewGuid():N}.json");
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
            Paths = new OpenApiPaths()
        };

        try
        {
            await OpenApiDocumentWriter.WriteAsync(document, outputPath, TestContext.Current.CancellationToken);

            await using var stream = File.OpenRead(outputPath);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("3.0.4", json.RootElement.GetProperty("openapi").GetString());
            Assert.Equal("Test API", json.RootElement.GetProperty("info").GetProperty("title").GetString());
            Assert.False(File.Exists(outputPath + ".tmp"));
        }
        finally
        {
            File.Delete(outputPath);
            File.Delete(outputPath + ".tmp");
        }
    }
}
