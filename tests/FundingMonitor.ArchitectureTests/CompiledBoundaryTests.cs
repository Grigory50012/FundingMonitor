using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using Xunit;

namespace FundingMonitor.ArchitectureTests;

public sealed class CompiledBoundaryTests
{
    [Theory]
    [InlineData("FundingMonitor.CurrentRates.Application", "public class Leak { public FundingMonitor.HistoricalRates.Infrastructure.Persistence.HistoricalRatesDbContext Value = null!; }", "foreign module")]
    [InlineData("FundingMonitor.CurrentRates.Application", "public class Leak { public FundingMonitor.CurrentRates.Infrastructure.Persistence.CurrentRatesDbContext Value = null!; }", "reverse layer")]
    [InlineData("FundingMonitor.CurrentRates.Domain", "public class Leak { public Microsoft.Extensions.Logging.ILogger Value = null!; }", "framework")]
    [InlineData("FundingMonitor.SharedKernel", "public class Leak { public Microsoft.EntityFrameworkCore.DbContext Value = null!; }", "framework")]
    [InlineData("FundingMonitor.Messaging", "namespace FundingMonitor.Messaging; internal record FundingRateChanged(decimal Rate);", "transport surface")]
    [InlineData("FundingMonitor.ApiHost", "namespace FundingMonitor.ApiHost.Services; internal class FundingCalculator { public decimal Calculate(decimal rate) => rate * 3; }", "host surface")]
    [InlineData("FundingMonitor.ApiHost", "namespace FundingMonitor.ApiHost.Shared.Middleware; internal class RatesRepository { }", "host surface")]
    [InlineData("FundingMonitor.Messaging", "namespace FundingMonitor.Messaging; public class RabbitMqOptions { public record FundingRateChanged(decimal Rate); }", "transport surface")]
    [InlineData("FundingMonitor.ApiHost", "public class Program { private class RatesRepository { } }", "host surface")]
    [InlineData("FundingMonitor.CurrentRates.Contracts", "public class Leak { public FundingMonitor.CurrentRates.Domain.CurrentFundingRate Value = null!; }", "reverse layer")]
    [InlineData("FundingMonitor.CurrentRates.Application", "public class Leak { public FundingMonitor.Messaging.Abstractions.IMessagePublisher Value = null!; }", "reverse layer")]
    public void Guard_RejectsCompiledViolationWithoutProjectReference(string assemblyName, string source, string expected)
    {
        var failures = ArchitectureGuard.CheckAssembly(Compile(assemblyName, source));
        Assert.Contains(failures, failure => failure.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("const string sql = \"SELECT * FROM historical_rates.\\\"InboxMessages\\\"\";")]
    [InlineData("var sql = $\"SELECT * FROM historical_rates.\\\"InboxMessages\\\" WHERE id = {id}\";")]
    [InlineData("const string sql = \"SELECT * FROM public.\\\"Rates\\\"\";")]
    [InlineData("const string sql = \"SET search_path TO historical_rates\";")]
    public void Guard_RejectsForeignSchemaSql(string statement)
    {
        Assert.NotEmpty(ArchitectureGuard.CheckSql($"class Query {{ void Read(int id) {{ {statement} }} }}", "current_rates"));
    }

    [Fact]
    public void Guard_RejectsForeignSchemaInEfModel()
    {
        var builder = new ModelBuilder();
        builder.Entity<SchemaProbe>().ToTable("Rates", "historical_rates").HasKey(entity => entity.Id);
        Assert.NotEmpty(ArchitectureGuard.CheckModel(builder.Model, "current_rates"));
    }

    [Theory]
    [InlineData("view")]
    [InlineData("sequence")]
    public void Guard_RejectsForeignViewOrSequence(string mapping)
    {
        var builder = new ModelBuilder();
        builder.HasDefaultSchema("current_rates");
        if (mapping == "view") builder.Entity<SchemaProbe>().ToView("Rates", "historical_rates");
        else builder.HasSequence<int>("RateSequence", "public");
        Assert.NotEmpty(ArchitectureGuard.CheckModel(builder.Model, "current_rates"));
    }

    [Theory]
    [InlineData("FundingMonitor.CurrentRates.Application", "public class Port { public FundingMonitor.ExchangeIntegration.Contracts.IExchangeMarketDataModule Value = null!; }")]
    [InlineData("FundingMonitor.CurrentRates.Infrastructure", "public class Port { public FundingMonitor.Messaging.Abstractions.IMessagePublisher Value = null!; }")]
    [InlineData("FundingMonitor.Messaging", "namespace FundingMonitor.Messaging.Abstractions; public interface IMessagePublisher { }")]
    [InlineData("FundingMonitor.ApiHost", "public class Program { public void Configure() { System.Action action = () => System.Console.WriteLine(1); action(); } }")]
    public void Guard_AcceptsAllowedCompiledBoundary(string assemblyName, string source) =>
        Assert.Empty(ArchitectureGuard.CheckAssembly(Compile(assemblyName, source)));

    [Fact]
    public void Guard_AcceptsOwnedSqlAndIgnoresComments()
    {
        const string source = """class Query { /* SELECT * FROM historical_rates.Rates */ const string Sql = "SELECT * FROM current_rates.Rates"; }""";
        Assert.Empty(ArchitectureGuard.CheckSql(source, "current_rates"));
    }

    [Fact]
    public void CompiledProductionAssemblies_ObeyModuleLayerAndSurfaceBoundaries()
    {
        var failures = ProductionAssemblies().SelectMany(path => ArchitectureGuard.CheckAssembly(File.ReadAllBytes(path)));
        Assert.True(!failures.Any(), string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void PersistenceImplementations_AreNotPublicModuleApis()
    {
        foreach (var context in new[] { typeof(CurrentRatesDbContext), typeof(HistoricalRatesDbContext) })
        {
            Assert.True(context.IsPublic, "DbContexts remain public for the standalone migrator");
            var repository = context.Assembly.GetTypes().Single(type => type.Name.EndsWith("RatesRepository", StringComparison.Ordinal));
            Assert.False(repository.IsPublic, $"{repository.FullName} must be internal");
        }
    }

    [Fact]
    public void DomainAndSharedKernelProjects_DeclareNoFrameworkOrPackageDependencies()
    {
        foreach (var project in Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.csproj", SearchOption.AllDirectories)
                     .Where(path => path.EndsWith(".Domain.csproj", StringComparison.Ordinal) || path.EndsWith("FundingMonitor.SharedKernel.csproj", StringComparison.Ordinal)))
        {
            var xml = System.Xml.Linq.XDocument.Load(project);
            Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "PackageReference" or "FrameworkReference");
        }
    }

    [Fact]
    public void ModulePersistence_ModelsAndSqlStayInOwnedSchemas()
    {
        using var current = new CurrentRatesDbContext(new DbContextOptionsBuilder<CurrentRatesDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        using var history = new HistoricalRatesDbContext(new DbContextOptionsBuilder<HistoricalRatesDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        Assert.Empty(ArchitectureGuard.CheckModel(current.Model, "current_rates"));
        Assert.Empty(ArchitectureGuard.CheckModel(history.Model, "historical_rates"));
        foreach (var (module, schema) in new[] { ("CurrentRates", "current_rates"), ("HistoricalRates", "historical_rates") })
        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "Modules", module), "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj")))
            Assert.Empty(ArchitectureGuard.CheckSql(File.ReadAllText(file), schema).Select(failure => $"{file}: {failure}"));
    }

    private static byte[] Compile(string assemblyName, string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => Path.GetFileNameWithoutExtension(path) != assemblyName)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(assemblyName, [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return stream.ToArray();
    }

    private static IEnumerable<string> ProductionAssemblies() =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(path) + ".dll"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FundingMonitor.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }

    private sealed class SchemaProbe { public int Id { get; set; } }
}
