using System.Text.Json;
using System.Text.RegularExpressions;

namespace Suwpptx.Server.Tests;

public sealed partial class AppSettingsTests
{
    private static readonly string[] Framework = ["Logging", "AllowedHosts", "ConnectionStrings"];

    [GeneratedRegex(@"BindConfiguration\(\s*""([^"":]+)")]
    private static partial Regex Binding();

    [Fact]
    public void Every_section_of_appsettings_is_bound_by_an_options_class()
    {
        var bound = SourceFile
            .All.SelectMany(file => Binding().Matches(file.Root.ToFullString()).Select(match => match.Groups[1].Value))
            .ToHashSet();
        var sections = RepositoryFiles
            .Paths()
            .Where(path =>
                Path.GetFileName(path).StartsWith("appsettings", StringComparison.Ordinal)
                && path.EndsWith(".json", StringComparison.Ordinal)
            )
            .SelectMany(path =>
                JsonDocument.Parse(RepositoryFiles.Read(path)).RootElement.EnumerateObject().Select(property => $"{property.Name} ({path})")
            )
            .Where(section => !Framework.Contains(section.Split(' ')[0]) && !bound.Contains(section.Split(' ')[0]))
            .Order()
            .ToList();

        Assert.True(
            sections.Count == 0,
            $"remove the setting or add the code that reads it (AddOptions<T>().BindConfiguration(\"<Section>\")): {string.Join(", ", sections)}"
        );
    }

    [Fact]
    public void No_connection_string_names_the_host_localhost()
    {
        var found = RepositoryFiles
            .Paths()
            .Where(path =>
                new[] { ".cs", ".json", ".sh", ".ts", ".yaml", ".md" }.Contains(Path.GetExtension(path))
                && !path.EndsWith("pnpm-lock.yaml", StringComparison.Ordinal)
            )
            .Where(path => RepositoryFiles.Read(path).Contains("Host=" + "localhost", StringComparison.OrdinalIgnoreCase))
            .Order()
            .ToList();

        Assert.True(
            found.Count == 0,
            $"write Host=127.0.0.1: on Windows `localhost` tries IPv6 first and every new connection waits seconds (the tests took 56 s instead of 20 s): {string.Join(", ", found)}"
        );
    }
}
