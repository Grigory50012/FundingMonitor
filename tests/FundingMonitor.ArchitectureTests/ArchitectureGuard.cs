using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FundingMonitor.ArchitectureTests;

internal static class ArchitectureGuard
{
    private static readonly HashSet<string> Modules = ["CurrentRates", "HistoricalRates", "ExchangeIntegration", "Arbitrage"];
    private static readonly Dictionary<string, string[]> AllowedLayers = new()
    {
        ["Contracts"] = [], ["Domain"] = [],
        ["Application"] = ["Contracts", "Domain"],
        ["Infrastructure"] = ["Contracts", "Domain", "Application"],
        ["Presentation"] = ["Contracts", "Domain", "Application", "Infrastructure"]
    };
    private static readonly HashSet<(string Owner, string Target)> AllowedCrossModuleReferences =
    [
        ("FundingMonitor.CurrentRates.Application", "FundingMonitor.ExchangeIntegration.Contracts"),
        ("FundingMonitor.HistoricalRates.Application", "FundingMonitor.ExchangeIntegration.Contracts"),
        ("FundingMonitor.HistoricalRates.Infrastructure", "FundingMonitor.CurrentRates.Contracts"),
        ("FundingMonitor.Arbitrage.Application", "FundingMonitor.CurrentRates.Contracts")
    ];
    // These small composition/transport surfaces are deliberately closed. New types require review.
    private static readonly HashSet<string> TransportTypes =
    [
        "DependencyInjection", "MessagingJson", "MessageEnvelope`1", "Abstractions.MessagePublishResult", "Abstractions.IMessagePublisher",
        "MessagingJson+ExchangeIdConverter", "MessagingJson+SymbolConverter",
        "RabbitMqConnection", "RabbitMqMessagePublisher", "RabbitMqOptions", "RabbitMqPublishException"
    ];
    private static readonly HashSet<string> HostTypes =
    [
        "Program", "FundingMonitor.ApiHost.Shared.Middleware.ExceptionHandlingMiddleware",
        "FundingMonitor.ApiHost.Shared.ProblemDetails.ApiProblemDetails",
        "FundingMonitor.ApiHost.Shared.ProblemDetails.ApiNotFoundProblemDetails",
        "FundingMonitor.ApiHost.Shared.ProblemDetails.ApiServerProblemDetails",
        "FundingMonitor.ApiHost.Shared.ProblemDetails.ApiServiceUnavailableProblemDetails",
        "FundingMonitor.ApiHost.Shared.ProblemDetails.ApiTimeoutProblemDetails",
        "FundingMonitor.ApiHost.Shared.ProblemDetails.ApiValidationProblemDetails"
    ];

    public static IReadOnlyList<string> CheckAssembly(byte[] assembly)
    {
        using var stream = new MemoryStream(assembly);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var owner = metadata.GetString(metadata.GetAssemblyDefinition().Name);
        var failures = new List<string>();
        foreach (var handle in metadata.AssemblyReferences)
        {
            var target = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
            var violation = ReferenceViolation(owner, target);
            if (violation is not null) failures.Add($"{owner} -> {target}: {violation}");
        }
        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            var name = metadata.GetString(type.Name);
            if (name.StartsWith('<')) continue; // Compiler-generated closures and async state machines.
            var fullName = FullName(metadata, handle);
            if (owner == "FundingMonitor.Messaging" && !TransportTypes.Any(allowed => fullName == $"{owner}.{allowed}"))
                failures.Add($"{fullName}: transport surface contains an unreviewed type");
            if (owner == "FundingMonitor.ApiHost" && !HostTypes.Contains(fullName))
                failures.Add($"{fullName}: host surface contains business or unreviewed types");
        }
        return failures;
    }

    private static string FullName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var type = metadata.GetTypeDefinition(handle);
        var name = metadata.GetString(type.Name);
        if (!type.GetDeclaringType().IsNil) return $"{FullName(metadata, type.GetDeclaringType())}+{name}";
        var ns = metadata.GetString(type.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private static string? ReferenceViolation(string owner, string target)
    {
        var sourceParts = owner.Split('.');
        var targetParts = target.Split('.');
        if (owner.EndsWith(".Domain", StringComparison.Ordinal) || owner == "FundingMonitor.SharedKernel")
        {
            if (target != "FundingMonitor.SharedKernel" && target != "netstandard" && target != "mscorlib"
                && !target.StartsWith("System.", StringComparison.Ordinal) && target != "System")
                return "framework or external dependency in domain/kernel";
        }
        if (!target.StartsWith("FundingMonitor.", StringComparison.Ordinal)) return null;
        if (owner == "FundingMonitor.Messaging")
            return target == "FundingMonitor.SharedKernel" ? null : "transport surface depends on business module";
        if (owner == "FundingMonitor.ApiHost")
            return targetParts.Length == 3 && Modules.Contains(targetParts[1]) && targetParts[2] == "Presentation"
                ? null : "host surface may only use module composition roots";
        if (sourceParts.Length != 3 || !Modules.Contains(sourceParts[1])) return null;
        if (targetParts.Length == 3 && Modules.Contains(targetParts[1]))
        {
            if (sourceParts[1] != targetParts[1])
                return AllowedCrossModuleReferences.Contains((owner, target))
                    ? null : "foreign module dependency outside the approved matrix";
            return AllowedLayers[sourceParts[2]].Contains(targetParts[2]) ? null : "reverse layer dependency";
        }
        if (target == "FundingMonitor.Messaging" && sourceParts[2] != "Infrastructure")
            return "reverse layer transport dependency";
        return null;
    }

    public static IReadOnlyList<string> CheckSql(string source, string ownedSchema)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        // Parse C# literals, not comments or using directives; handles raw and interpolated SQL as well.
        var strings = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
            .Where(node => node.IsKind(SyntaxKind.StringLiteralExpression)).Select(node => node.Token.ValueText)
            .Concat(root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>().Select(node =>
                string.Concat(node.Contents.OfType<InterpolatedStringTextSyntax>().Select(text => text.TextToken.ValueText))));
        var failures = new List<string>();
        foreach (var sql in strings)
        {
            if (Regex.IsMatch(sql, @"\bsearch_path\b", RegexOptions.IgnoreCase))
                failures.Add("Repositories must not change search_path");
            foreach (Match match in Regex.Matches(sql,
                         "\\b(?:FROM|JOIN|INTO|UPDATE|TABLE)\\s+(?:\"(?<schema>[^\"]+)\"|(?<schema>\\w+))\\s*\\.", RegexOptions.IgnoreCase))
                if (!string.Equals(match.Groups["schema"].Value, ownedSchema, StringComparison.Ordinal))
                    failures.Add($"SQL accesses foreign schema {match.Groups["schema"].Value}");
            foreach (var knownSchema in new[] { "current_rates", "historical_rates", "exchange_integration", "arbitrage" })
                if (knownSchema != ownedSchema && Regex.IsMatch(sql, $@"\b{knownSchema}\b"))
                    failures.Add($"Literal references foreign schema {knownSchema}");
        }
        return failures;
    }

    public static IReadOnlyList<string> CheckModel(IReadOnlyModel model, string ownedSchema)
    {
        var failures = new List<string>();
        foreach (var entity in model.GetEntityTypes())
        {
            if (entity.GetTableName() is not null && entity.GetSchema() != ownedSchema)
                failures.Add($"{entity.Name}: table schema {entity.GetSchema()} is not {ownedSchema}");
            if (entity.GetViewName() is not null && entity.GetViewSchema() != ownedSchema)
                failures.Add($"{entity.Name}: foreign view schema");
            if (entity.GetSqlQuery() is not null)
                failures.Add($"{entity.Name}: SQL query model mappings require explicit boundary review");
        }
        foreach (var sequence in model.GetSequences())
            if (sequence.Schema != ownedSchema) failures.Add($"{sequence.Name}: foreign sequence schema");
        return failures;
    }
}
