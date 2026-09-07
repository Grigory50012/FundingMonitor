using System.Xml.Linq;
using Xunit;

namespace FundingMonitor.ArchitectureTests;

public sealed class ProjectReferenceRulesTests
{
    private static readonly string Root = FindRepositoryRoot();

    [Fact]
    public void ProductionProjects_UseOnlyAllowedReferences()
    {
        var allowed = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["FundingMonitor.SharedKernel"] = [],
            ["FundingMonitor.Messaging"] = ["FundingMonitor.SharedKernel"],
            ["FundingMonitor.ApiHost"] =
            [
                "FundingMonitor.ExchangeIntegration.Presentation",
                "FundingMonitor.CurrentRates.Presentation",
                "FundingMonitor.HistoricalRates.Presentation",
                "FundingMonitor.Arbitrage.Presentation"
            ],
            ["FundingMonitor.DatabaseMigrator"] =
            [
                "FundingMonitor.CurrentRates.Infrastructure",
                "FundingMonitor.HistoricalRates.Infrastructure"
            ]
        };

        foreach (var module in new[]
                 {
                     "ExchangeIntegration", "CurrentRates", "HistoricalRates", "Arbitrage"
                 })
        {
            allowed[$"FundingMonitor.{module}.Contracts"] = ["FundingMonitor.SharedKernel"];
            allowed[$"FundingMonitor.{module}.Domain"] = ["FundingMonitor.SharedKernel"];
            allowed[$"FundingMonitor.{module}.Application"] =
            [
                $"FundingMonitor.{module}.Contracts",
                $"FundingMonitor.{module}.Domain",
                "FundingMonitor.SharedKernel"
            ];
            allowed[$"FundingMonitor.{module}.Infrastructure"] =
            [
                $"FundingMonitor.{module}.Contracts",
                $"FundingMonitor.{module}.Domain",
                $"FundingMonitor.{module}.Application",
                "FundingMonitor.SharedKernel"
            ];
            allowed[$"FundingMonitor.{module}.Presentation"] =
            [
                $"FundingMonitor.{module}.Contracts",
                $"FundingMonitor.{module}.Application",
                $"FundingMonitor.{module}.Infrastructure"
            ];
        }

        allowed["FundingMonitor.CurrentRates.Application"] =
        [
            .. allowed["FundingMonitor.CurrentRates.Application"],
            "FundingMonitor.ExchangeIntegration.Contracts"
        ];
        allowed["FundingMonitor.HistoricalRates.Application"] =
        [
            .. allowed["FundingMonitor.HistoricalRates.Application"],
            "FundingMonitor.ExchangeIntegration.Contracts"
        ];
        allowed["FundingMonitor.HistoricalRates.Infrastructure"] =
        [
            .. allowed["FundingMonitor.HistoricalRates.Infrastructure"],
            "FundingMonitor.CurrentRates.Contracts", "FundingMonitor.Messaging"
        ];
        allowed["FundingMonitor.CurrentRates.Infrastructure"] =
        [
            .. allowed["FundingMonitor.CurrentRates.Infrastructure"],
            "FundingMonitor.Messaging"
        ];
        allowed["FundingMonitor.Arbitrage.Application"] =
        [
            .. allowed["FundingMonitor.Arbitrage.Application"],
            "FundingMonitor.CurrentRates.Contracts"
        ];

        foreach (var (project, expected) in allowed)
        {
            var actual = ReadReferences(project);
            Assert.Equal(expected.Order(), actual.Order());
        }
    }

    private static string[] ReadReferences(string projectName)
    {
        var projectFile = Directory
            .EnumerateFiles(Path.Combine(Root, "src"), $"{projectName}.csproj", SearchOption.AllDirectories)
            .Single();
        var document = XDocument.Load(projectFile);
        return document.Descendants("ProjectReference")
            .Select(node => Path.GetFileNameWithoutExtension(node.Attribute("Include")!.Value))
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FundingMonitor.sln")))
            directory = directory.Parent;
        return directory?.FullName
               ?? throw new DirectoryNotFoundException("FundingMonitor repository root was not found.");
    }
}