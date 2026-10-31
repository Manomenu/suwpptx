using System.Xml.Linq;

namespace Suwpptx.Server.Tests;

public sealed class ProjectReferencesTests
{
    private sealed record Allowed(string[] References, string[] InternalsVisibleTo);

    private static readonly Dictionary<string, Allowed> Table = new()
    {
        ["Suwpptx.Server"] = new([], ["Suwpptx.Server.Tests"]),
        ["Suwpptx.Server.Tests"] = new(["Suwpptx.Server"], []),
    };

    private static string Attr(XElement element, string name) =>
        (string?)element.Attribute(name) ?? throw new InvalidOperationException($"<{element.Name}> has no {name} attribute");

    private static Dictionary<string, XDocument> Projects() =>
        XDocument
            .Load(Path.Combine(RepositoryFiles.Root, "Suwpptx.slnx"))
            .Descendants("Project")
            .Select(project => Path.Combine(RepositoryFiles.Root, Attr(project, "Path")))
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path), XDocument.Load);

    public static TheoryData<string> ProjectNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in Table.Keys)
        {
            data.Add(name);
        }
        return data;
    }

    [Fact]
    public void Every_project_of_the_solution_is_in_the_table()
    {
        var unknown = Projects().Keys.Except(Table.Keys).Order().ToList();
        var missing = Table.Keys.Except(Projects().Keys).Order().ToList();

        Assert.True(unknown.Count == 0, $"add to Table in ProjectReferencesTests.cs what it may reference: {string.Join(", ", unknown)}");
        Assert.True(missing.Count == 0, $"in Table but not in Suwpptx.slnx, remove from Table: {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(ProjectNames))]
    public void A_project_references_only_the_projects_the_table_allows(string project)
    {
        var references = Projects()[project]
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(Attr(reference, "Include").Replace('\\', '/')))
            .Order()
            .ToList();

        var forbidden = references.Except(Table[project].References).ToList();
        Assert.True(
            forbidden.Count == 0,
            $"{project} must not reference {string.Join(", ", forbidden)}: move the shared code down, or change Table in ProjectReferencesTests.cs and say why"
        );
        var missing = Table[project].References.Except(references).ToList();
        Assert.True(missing.Count == 0, $"{project} no longer references {string.Join(", ", missing)}: update Table");
    }

    [Theory]
    [MemberData(nameof(ProjectNames))]
    public void A_project_shows_its_internals_only_to_the_projects_the_table_allows(string project)
    {
        var visibleTo = Projects()[project].Descendants("InternalsVisibleTo").Select(item => Attr(item, "Include")).Order().ToList();

        var forbidden = visibleTo.Except(Table[project].InternalsVisibleTo).ToList();
        Assert.True(
            forbidden.Count == 0,
            $"{project} must not show its internals to {string.Join(", ", forbidden)}: update Table if intended"
        );
        var missing = Table[project].InternalsVisibleTo.Except(visibleTo).ToList();
        Assert.True(missing.Count == 0, $"{project} no longer shows its internals to {string.Join(", ", missing)}: update Table");
    }
}
