using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Suwpptx.Server.Tests;

public sealed partial class PluginTests
{
    private static readonly string[] Skills = ["make", "brief", "template"];

    [GeneratedRegex(@"^name:\s*(?<value>\S.*?)\s*$", RegexOptions.Multiline)]
    private static partial Regex NameLine();

    [GeneratedRegex(@"^description:\s*\S", RegexOptions.Multiline)]
    private static partial Regex DescriptionLine();

    [Fact]
    [Trait("criterion", "AC-pptx-studio-17")]
    public void The_plugin_is_called_suwpptx_and_the_marketplace_points_at_it()
    {
        var plugin = JsonDocument.Parse(RepositoryFiles.Read("plugin/.claude-plugin/plugin.json")).RootElement;
        var marketplace = JsonDocument.Parse(RepositoryFiles.Read(".claude-plugin/marketplace.json")).RootElement;

        Assert.Equal("suwpptx", plugin.GetProperty("name").GetString());
        var listed = Assert.Single(
            marketplace.GetProperty("plugins").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "suwpptx"
        );
        Assert.Equal("./plugin", listed.GetProperty("source").GetString());
    }

    [Theory]
    [Trait("criterion", "AC-pptx-studio-17")]
    [InlineData("make")]
    [InlineData("brief")]
    [InlineData("template")]
    public void Every_skill_has_a_folder_name_and_a_description(string skill)
    {
        var text = RepositoryFiles.Read($"plugin/skills/{skill}/SKILL.md");
        var parts = text.Split("---", 3);

        Assert.True(
            parts.Length == 3 && parts[0].Trim().Length == 0,
            $"plugin/skills/{skill}/SKILL.md must start with a --- frontmatter block"
        );
        Assert.Equal(skill, NameLine().Match(parts[1]).Groups["value"].Value);
        Assert.True(DescriptionLine().IsMatch(parts[1]), $"plugin/skills/{skill}/SKILL.md needs a non-empty description:");
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-17")]
    public void There_are_exactly_the_skills_make_brief_and_template()
    {
        var folders = Directory.GetDirectories(Path.Combine(RepositoryFiles.Root, "plugin", "skills")).Select(Path.GetFileName).Order();

        Assert.Equal(Skills.Order(), folders);
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-17")]
    public async Task Claude_Code_validates_the_plugin()
    {
        const string Missing = "Install Claude Code to validate the plugin";
        if (!OnPath("claude"))
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
            {
                Assert.Fail(Missing);
            }
            Assert.Skip(Missing);
        }
        var info = new ProcessStartInfo("claude")
        {
            WorkingDirectory = RepositoryFiles.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("plugin");
        info.ArgumentList.Add("validate");
        info.ArgumentList.Add("./plugin");
        using var process = Process.Start(info) ?? throw new InvalidOperationException("claude did not start");
        var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var errors = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        limit.CancelAfter(TimeSpan.FromSeconds(120));

        await process.WaitForExitAsync(limit.Token);

        Assert.True(process.ExitCode == 0, $"claude plugin validate ./plugin failed:{Environment.NewLine}{await output}{await errors}");
    }

    private static bool OnPath(string program) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(directory => File.Exists(Path.Combine(directory, program)));
}
