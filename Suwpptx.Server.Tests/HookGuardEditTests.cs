using System.Diagnostics;
using System.Text.Json;

namespace Suwpptx.Server.Tests;

public sealed class HookGuardEditTests
{
    private static (int ExitCode, string Errors) Run(string path, bool allowGateEdit = false)
    {
        var gitBash = @"C:\Program Files\Git\bin\bash.exe";
        var start = new ProcessStartInfo(File.Exists(gitBash) ? gitBash : "bash")
        {
            RedirectStandardInput = true,
            RedirectStandardError = true,
            WorkingDirectory = RepositoryFiles.Root,
        };
        start.ArgumentList.Add("scripts/.internal/hook-guard-edit.sh");
        start.Environment.Remove("ALLOW_GATE_EDIT");
        if (allowGateEdit)
        {
            start.Environment["ALLOW_GATE_EDIT"] = "1";
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException("bash did not start");
        process.StandardInput.Write(
            JsonSerializer.Serialize(new { tool_name = "Edit", tool_input = new { file_path = $"{RepositoryFiles.Root}/{path}" } })
        );
        process.StandardInput.Close();
        var errors = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, errors);
    }

    [Theory]
    [InlineData("scripts/.internal/check.sh")]
    [InlineData("scripts/.internal/criteria.sh")]
    [InlineData("scripts/.internal/hook-guard-edit.sh")]
    [InlineData("criteria.json")]
    [InlineData(".claude/settings.json")]
    [InlineData(".github/workflows/ci.yml")]
    [InlineData(".editorconfig")]
    [InlineData("Suwpptx.Web/eslint.config.js")]
    [InlineData("Suwpptx.Web/tsconfig.app.json")]
    [InlineData("BannedSymbols.txt")]
    [InlineData("Directory.Build.props")]
    public void An_agent_cannot_edit_the_gate_and_is_told_to_ask_the_owner(string path)
    {
        var (exitCode, errors) = Run(path);

        Assert.Equal(2, exitCode);
        Assert.Contains("ALLOW_GATE_EDIT=1 claude", errors, StringComparison.Ordinal);
        Assert.Contains("ask the user", errors, StringComparison.Ordinal);
    }

    [Fact]
    public void The_owner_lets_a_session_edit_the_gate() =>
        Assert.Equal(0, Run("scripts/.internal/check.sh", allowGateEdit: true).ExitCode);

    [Theory]
    [InlineData("Suwpptx.Server/Notes/NoteStore.cs")]
    [InlineData("Suwpptx.Web/src/notes/notes.test.ts")]
    [InlineData("AGENTS.md")]
    public void Everything_else_stays_editable(string path) => Assert.Equal(0, Run(path).ExitCode);
}
