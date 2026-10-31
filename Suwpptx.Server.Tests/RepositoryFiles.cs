using System.Diagnostics;

namespace Suwpptx.Server.Tests;

internal static class RepositoryFiles
{
    public static string Root { get; } = FindRoot();

    public static string[] Paths()
    {
        var git = new ProcessStartInfo("git", "ls-files --cached --others --exclude-standard")
        {
            WorkingDirectory = Root,
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(git) ?? throw new InvalidOperationException("git did not start");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        return [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(path => path.TrimEnd('\r'))];
    }

    public static IEnumerable<string> CSharpSources() =>
        Paths()
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal))
            .Where(path => !path.Contains("/obj/", StringComparison.Ordinal) && !path.Contains("/bin/", StringComparison.Ordinal))
            .Where(path => !path.EndsWith(".g.cs", StringComparison.Ordinal) && !path.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(path => File.Exists(Path.Combine(Root, path)));

    public static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Suwpptx.slnx")))
            {
                return directory.FullName;
            }
        }
        throw new InvalidOperationException("Suwpptx.slnx not found above " + AppContext.BaseDirectory);
    }
}
