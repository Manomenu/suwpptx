using System.Globalization;
using System.Text.RegularExpressions;
using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Workspaces;

internal static partial class WorkspaceFolder
{
    public const string Context = "context.md";

    public const string ContextTemplate = """
        # Requirements

        ## Audience

        ## Goal

        ## Must include

        ## Style

        """;

    [GeneratedRegex(@"^v([0-9]{1,6})\.pptx$")]
    private static partial Regex VersionFile();

    public static void Create(string directory)
    {
        Directory.CreateDirectory(Path.Combine(directory, "resources"));
        Directory.CreateDirectory(Path.Combine(directory, "versions"));
        File.WriteAllText(Path.Combine(directory, Context), ContextTemplate);
    }

    public static IReadOnlyList<FolderEntry> Files(string directory)
    {
        var context = new FileInfo(Path.Combine(directory, Context));
        return
        [
            .. context.Exists ? [FolderFiles.Entry(context, directory)] : Array.Empty<FolderEntry>(),
            .. FolderFiles.List(Path.Combine(directory, "resources"), directory),
        ];
    }

    public static IReadOnlyList<DeckVersion> Versions(string directory)
    {
        var versions = new DirectoryInfo(Path.Combine(directory, "versions"));
        if (!versions.Exists)
        {
            return [];
        }
        return
        [
            .. versions
                .EnumerateFiles("v*.pptx")
                .Select(file => (file, match: VersionFile().Match(file.Name)))
                .Where(item => item.match.Success)
                .Select(item => new DeckVersion(
                    int.Parse(item.match.Groups[1].Value, CultureInfo.InvariantCulture),
                    $"versions/{item.file.Name}",
                    item.file.Length,
                    new DateTimeOffset(item.file.LastWriteTimeUtc)
                ))
                .OrderByDescending(version => version.Number),
        ];
    }

    public static WorkspaceSummary Summary(string directory)
    {
        var files = Files(directory);
        var versions = Versions(directory);
        var changedAt = files
            .Select(file => file.ChangedAt)
            .Concat(versions.Select(version => version.ChangedAt))
            .Append(new DateTimeOffset(Directory.GetLastWriteTimeUtc(directory)))
            .Max();
        return new WorkspaceSummary(
            Path.GetFileName(directory),
            files.Count,
            versions.Count,
            versions.Count > 0 ? versions[0].Number : null,
            changedAt
        );
    }
}
