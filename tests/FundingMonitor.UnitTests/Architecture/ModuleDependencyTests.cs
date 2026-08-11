using System.Text.RegularExpressions;
using Xunit;

namespace FundingMonitor.UnitTests.Architecture;

public sealed class ModuleDependencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Theory]
    [InlineData("FundingMonitor.Core")]
    [InlineData("FundingMonitor.Application")]
    [InlineData("FundingMonitor.Infrastructure")]
    [InlineData("FundingMonitor.Api")]
    public void ProductionNamespacesMustBeOrganizedByModule(string projectName)
    {
        var invalidFiles = GetSourceFiles(projectName)
            .Where(file => !UsesAllowedNamespace(file, projectName))
            .Select(file => Path.GetRelativePath(RepositoryRoot, file))
            .ToArray();

        Assert.Empty(invalidFiles);
    }

    [Theory]
    [InlineData("FundingMonitor.Core")]
    [InlineData("FundingMonitor.Application")]
    [InlineData("FundingMonitor.Infrastructure")]
    public void HistoricalRatesMustNotReferenceCurrentRates(string projectName)
    {
        var violations = GetSourceFiles(projectName)
            .Where(file => GetNamespace(file).Contains(".HistoricalRates", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file).Contains(".CurrentRates", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(RepositoryRoot, file))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void CoreProjectMustNotReferenceOuterLayers()
    {
        var projectFile = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "FundingMonitor.Core",
            "FundingMonitor.Core.csproj"));

        Assert.DoesNotContain("FundingMonitor.Application", projectFile, StringComparison.Ordinal);
        Assert.DoesNotContain("FundingMonitor.Infrastructure", projectFile, StringComparison.Ordinal);
        Assert.DoesNotContain("FundingMonitor.Api", projectFile, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgramMustComposeExplicitModules()
    {
        var program = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "FundingMonitor.Api",
            "Program.cs"));

        Assert.Contains("AddCurrentRatesModule", program, StringComparison.Ordinal);
        Assert.Contains("AddHistoricalRatesModule", program, StringComparison.Ordinal);
        Assert.Contains("AddSharedInfrastructure", program, StringComparison.Ordinal);
    }

    private static IEnumerable<string> GetSourceFiles(string projectName)
    {
        var projectDirectory = Path.Combine(RepositoryRoot, "src", projectName);

        return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .Where(file => Path.GetFileName(file) is not "Program.cs" and not "GlobalUsings.cs");
    }

    private static bool UsesAllowedNamespace(string file, string projectName)
    {
        var namespaceName = GetNamespace(file);
        var allowedNamespaces = projectName switch
        {
            "FundingMonitor.Api" => new[] { ".CurrentRates", ".HistoricalRates", ".Shared" },
            _ => new[] { ".CurrentRates", ".HistoricalRates", ".Shared" }
        };

        return allowedNamespaces.Any(segment =>
            namespaceName.StartsWith(projectName + segment, StringComparison.Ordinal));
    }

    private static string GetNamespace(string file)
    {
        var match = Regex.Match(
            File.ReadAllText(file),
            @"^namespace\s+([^;\s{]+)",
            RegexOptions.Multiline);

        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FundingMonitor.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("FundingMonitor repository root was not found.");
    }
}
