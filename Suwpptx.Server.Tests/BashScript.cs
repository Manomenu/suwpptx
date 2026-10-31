using System.Diagnostics;

namespace Suwpptx.Server.Tests;

internal static class BashScript
{
    public static bool OnPath(string program) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(directory => File.Exists(System.IO.Path.Combine(directory, program)));

    // Why: locally a missing tool skips the test; in CI (the CI variable is set) it fails, so a gate never passes unchecked.
    public static void Require(params string[] programs)
    {
        var missing = programs.Where(program => !OnPath(program)).ToList();
        if (missing.Count == 0)
        {
            return;
        }
        var message = $"Install {string.Join(", ", missing)} to run this test";
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
        {
            Assert.Fail(message);
        }
        Assert.Skip(message);
    }

    public static void MakeExecutable(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    public static async Task<ScriptResult> RunAsync(
        string script,
        string workingDirectory,
        IReadOnlyDictionary<string, string?> environment,
        CancellationToken cancellationToken,
        TimeSpan? limit = null
    )
    {
        var info = new ProcessStartInfo("bash")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add(script);
        foreach (var (key, value) in environment)
        {
            if (value is null)
            {
                info.Environment.Remove(key);
            }
            else
            {
                info.Environment[key] = value;
            }
        }
        var clock = Stopwatch.StartNew();
        using var process = Process.Start(info) ?? throw new InvalidOperationException("bash did not start");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errors = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(limit ?? TimeSpan.FromMinutes(3));
        await process.WaitForExitAsync(timeout.Token);
        return new ScriptResult(process.ExitCode, await output, await errors, clock.Elapsed);
    }
}
