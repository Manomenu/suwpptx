using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Suwpptx.Server.Tests;

public sealed partial class RepositoryLayoutTests
{
    private const string Map = "docs/architecture.md";
    private const int MaxMapLineLength = 300;

    [GeneratedRegex(@"^- `([^`]+)`")]
    private static partial Regex MapEntry();

    [Fact]
    public void Every_top_level_folder_is_in_the_map_of_architecture_md()
    {
        var folders = RepositoryFiles
            .Paths()
            .Where(path => path.Contains('/', StringComparison.Ordinal))
            .Select(path => path.Split('/')[0])
            .ToHashSet();
        var described = MapLines().Select(line => MapEntry().Match(line).Groups[1].Value.Split('/')[0]).ToHashSet();

        var missing = folders.Except(described).Order(StringComparer.Ordinal).ToList();
        var stale = described
            .Where(entry => !folders.Contains(entry) && !File.Exists(Path.Combine(RepositoryFiles.Root, entry)))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0, $"add a line to the map in {Map} (- `Folder/` - what it holds) for: {string.Join(", ", missing)}");
        Assert.True(stale.Count == 0, $"the map in {Map} names paths that do not exist: delete the lines for {string.Join(", ", stale)}");
    }

    [Fact]
    public void Every_line_of_the_map_is_short()
    {
        var tooLong = MapLines().Where(line => line.Length > MaxMapLineLength).Select(line => line[..40]).ToList();

        Assert.True(
            tooLong.Count == 0,
            $"shorten these lines of the map in {Map} to {MaxMapLineLength} characters: {string.Join("; ", tooLong)}"
        );
    }

    [Fact]
    public void No_two_files_differ_only_in_letter_case()
    {
        var clashes = RepositoryFiles
            .Paths()
            .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => string.Join(" vs ", group.Order(StringComparer.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(clashes.Count == 0, $"rename one of each pair (Windows and macOS ignore case): {string.Join("; ", clashes)}");
    }

    [Fact]
    public void Every_shell_script_is_executable_in_git()
    {
        // Why: the index, not the file system — a checkout on Windows or with core.fileMode=false has no exec bit to read.
        var git = new ProcessStartInfo("git", "ls-files --stage -- *.sh")
        {
            WorkingDirectory = RepositoryFiles.Root,
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(git) ?? throw new InvalidOperationException("git did not start");
        var notExecutable = process
            .StandardOutput.ReadToEnd()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith("100755", StringComparison.Ordinal))
            .Select(line => line.Split('\t')[1].TrimEnd('\r'))
            .ToList();
        process.WaitForExit();

        Assert.True(notExecutable.Count == 0, $"make them executable: git update-index --chmod=+x {string.Join(" ", notExecutable)}");
    }

    private static string[] MapLines() =>
        [.. RepositoryFiles.Read(Map).Split('\n').Select(line => line.TrimEnd('\r')).Where(line => MapEntry().IsMatch(line))];
}
