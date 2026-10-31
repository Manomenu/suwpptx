using System.Diagnostics;

namespace Suwpptx.Server.Tests;

public sealed class LogsScriptTests
{
    private const string TraceId = "642935176ff7c3aa085f1e5d12a76382";

    private static string Run(params string[] arguments)
    {
        var gitBash = @"C:\Program Files\Git\bin\bash.exe";
        var start = new ProcessStartInfo(File.Exists(gitBash) ? gitBash : "bash")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("scripts/.internal/logs.sh");
        start.ArgumentList.Add("--file");
        start.ArgumentList.Add("Suwpptx.Server.Tests/LogSamples/server.jsonl");
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        start.WorkingDirectory = RepositoryFiles.Root;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("bash did not start");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, process.StandardError.ReadToEnd());
        return output;
    }

    [Fact]
    public void By_default_only_warnings_and_errors_show_one_compact_line_each()
    {
        var lines = Run().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
        Assert.Contains(
            $"Warning     Suwpptx.Server.Health trace={TraceId} The health check could not reach the database",
            lines[1],
            StringComparison.Ordinal
        );
        Assert.Contains("| Npgsql.NpgsqlException", lines[1], StringComparison.Ordinal);
        Assert.DoesNotContain("\n", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void All_shows_every_level() => Assert.Contains("Information", Run("--all"), StringComparison.Ordinal);

    [Fact]
    public void A_trace_shows_the_lines_of_that_request_in_either_spelling()
    {
        Assert.Contains(TraceId, Run("--trace", TraceId), StringComparison.Ordinal);
        Assert.Contains(TraceId, Run("--trace", $"00-{TraceId}-0123456789abcdef-00"), StringComparison.Ordinal);
    }
}
