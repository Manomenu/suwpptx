using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Suwpptx.Server.Api;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Suwpptx.Server.Tests;

public sealed class ContractsTests
{
    private const string Root = "Suwpptx.Server";

    private static readonly string[] Layers = ["Api", "Workspaces | Templates | Previews | Changes | Health", "Folder"];

    internal static readonly Architecture Server = new ArchLoader().LoadAssemblies(typeof(ServerApp).Assembly).Build();

    public static IEnumerable<(string Part, string Forbidden)> EdgesOf(string[] layers)
    {
        var levels = layers.Select(layer => layer.Split('|', StringSplitOptions.TrimEntries)).ToArray();
        for (var level = 0; level < levels.Length; level++)
        {
            foreach (var part in levels[level])
            {
                foreach (
                    var forbidden in levels.Take(level).SelectMany(above => above).Concat(levels[level].Where(sibling => sibling != part))
                )
                {
                    yield return (part, forbidden);
                }
            }
        }
    }

    public static TheoryData<string, string> Edges()
    {
        var data = new TheoryData<string, string>();
        foreach (var (part, forbidden) in EdgesOf(Layers))
        {
            data.Add(part, forbidden);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Edges))]
    public void Layers_use_only_the_layers_below_them(string part, string forbidden) =>
        Types()
            .That()
            .ResideInNamespaceMatching(Pattern(part))
            .Should()
            .NotDependOnAny(Types().That().ResideInNamespaceMatching(Pattern(forbidden)))
            .Because(
                $"{part} must not use {forbidden}: move the shared code down to a shared layer or pass it in from above (Layers in ContractsTests.cs)"
            )
            .WithoutRequiringPositiveResults()
            .Check(Server);

    [Fact]
    public void Every_server_namespace_has_a_layer()
    {
        var layered = Layers.SelectMany(layer => layer.Split('|', StringSplitOptions.TrimEntries)).ToHashSet();
        var missing = Server
            .Namespaces.Select(ns => ns.FullName)
            .Where(name => name.StartsWith(Root + ".", StringComparison.Ordinal))
            .Select(name => name[(Root.Length + 1)..].Split('.')[0])
            .Where(feature => !layered.Contains(feature))
            .Distinct()
            .Order()
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"add the namespace to Layers in ContractsTests.cs, at the place it belongs: {string.Join(", ", missing)}. If this starts a second product area, see docs/architecture.md#areas-when-a-second-product-area-appears."
        );
    }

    [Fact]
    public void Every_endpoints_class_is_mapped_from_the_Api_namespace()
    {
        var unmapped = Server
            .Types.Where(type => type.Name.EndsWith("Endpoints", StringComparison.Ordinal))
            .Where(type =>
                !type.BackwardsDependencies.Select(link => link.Origin).Any(user => user.Namespace.FullName == typeof(ServerApp).Namespace)
            )
            .Select(type => type.FullName)
            .Order()
            .ToArray();

        Assert.True(
            unmapped.Length == 0,
            $"call its Map method in Api/ServerApp.cs so that grep finds every route's wiring in one place: {string.Join(", ", unmapped)}"
        );
    }

    [Fact]
    public void Every_namespace_has_at_most_one_endpoints_class()
    {
        var crowded = typeof(ServerApp)
            .Assembly.DefinedTypes.Where(type => type.Name.EndsWith("Endpoints", StringComparison.Ordinal))
            .GroupBy(type => type.Namespace)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(type => type.Name).Order())}")
            .ToArray();

        Assert.True(
            crowded.Length == 0,
            $"a second set of routes is a second feature: give it a folder and a layer of its own: {string.Join("; ", crowded)}"
        );
    }

    private static string Pattern(string part) => $@"^{Regex.Escape(Root + "." + part)}(\..+)?$";
}
