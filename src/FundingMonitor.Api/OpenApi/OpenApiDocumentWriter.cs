using System.Text;
using Microsoft.OpenApi;

namespace FundingMonitor.Api.OpenApi;

public static class OpenApiDocumentWriter
{
    public static async Task WriteAsync(OpenApiDocument document, string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        var temporaryPath = fullPath + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                             4096, FileOptions.Asynchronous))
            await using (var textWriter = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                var openApiWriter = new OpenApiJsonWriter(textWriter);
                document.SerializeAsV3(openApiWriter);
                await textWriter.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, fullPath, true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
