namespace Suwpptx.Server.Tests;

public sealed class PythonFinderTests
{
    [Theory]
    [Trait("criterion", "AC-windows-compatibility-8")]
    [InlineData("python3", "python3")]
    [InlineData("python", "python")]
    [InlineData("py", "py -3")]
    [InlineData("python3,python", "python3")]
    [InlineData("python,py", "python")]
    public async Task The_finder_prints_the_first_of_python3_python_and_py_that_imports_pptx(string working, string expected)
    {
        ArgumentNullException.ThrowIfNull(working);
        BashScript.Require("bash");
        using var scratch = new ScratchFolder();

        var result = await Find(scratch, working.Split(','));

        Assert.True(result.ExitCode == 0, $"python.sh must exit 0 when one candidate imports pptx: {result.All}");
        Assert.Equal(expected, result.Output.Trim());
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-8")]
    public async Task When_none_imports_pptx_the_finder_lists_the_three_it_tried_and_the_install_commands()
    {
        BashScript.Require("bash");
        using var scratch = new ScratchFolder();

        var result = await Find(scratch, []);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output.Trim());
        Assert.Contains("python3, python, py -3", result.Errors, StringComparison.Ordinal);
        Assert.Contains("winget install Python.Python.3.12", result.Errors, StringComparison.Ordinal);
        Assert.Contains("python3 -m pip install --user python-pptx", result.Errors, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("criterion", "AC-windows-compatibility-8")]
    [InlineData("make")]
    [InlineData("template")]
    public void The_skills_that_run_Python_ask_the_finder_and_never_name_python3_themselves(string skill)
    {
        var text = RepositoryFiles.Read($"plugin/skills/{skill}/SKILL.md");

        Assert.True(
            text.Contains("scripts/python.sh", StringComparison.Ordinal),
            $"plugin/skills/{skill}/SKILL.md must call bash \"${{CLAUDE_PLUGIN_ROOT}}/scripts/python.sh\" and use its output as the Python command"
        );
        Assert.False(
            text.Contains("python3 -c", StringComparison.Ordinal),
            $"plugin/skills/{skill}/SKILL.md must not run python3 directly (Windows has no python3): use the finder's output"
        );
    }

    private static async Task<ScriptResult> Find(ScratchFolder scratch, string[] working)
    {
        var bin = scratch.Combine("bin");
        Directory.CreateDirectory(bin);
        foreach (var name in new[] { "python3", "python", "py" })
        {
            var works = working.Contains(name);
            // Why: a failing fake stands for the Microsoft Store placeholder; "py" is only the launcher, so it needs -3 first.
            var arguments =
                name == "py"
                    ? "[ \"$1\" = -3 ] && [ \"$2\" = -c ] && [ \"$3\" = \"import pptx\" ]"
                    : "[ \"$1\" = -c ] && [ \"$2\" = \"import pptx\" ]";
            var body = works ? $"#!/bin/sh\n{arguments} && exit 0\nexit 1\n" : "#!/bin/sh\nexit 1\n";
            var file = Path.Combine(bin, name);
            await File.WriteAllTextAsync(file, body, TestContext.Current.CancellationToken);
            BashScript.MakeExecutable(file);
        }
        return await BashScript.RunAsync(
            Path.Combine(RepositoryFiles.Root, "plugin", "scripts", "python.sh"),
            scratch.Path,
            new Dictionary<string, string?> { ["PATH"] = bin },
            TestContext.Current.CancellationToken
        );
    }
}
