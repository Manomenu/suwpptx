using System.Text.RegularExpressions;

namespace Suwpptx.Server.Tests;

public sealed partial class ReleaseWorkflowTests
{
    [GeneratedRegex(@"tags:\s*\[\s*""v\*""\s*\]")]
    private static partial Regex VersionTags();

    [GeneratedRegex(@"^  windows:\s*\n(?<body>(?:    .*\n|\s*\n)*)", RegexOptions.Multiline)]
    private static partial Regex WindowsJob();

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-1")]
    public void Release_workflow_publishes_both_zips_and_install_sh_when_a_version_tag_is_pushed()
    {
        var release = RepositoryFiles.Read(".github/workflows/release.yml");

        Assert.True(
            VersionTags().IsMatch(release),
            "release.yml must trigger on `push: tags: [\"v*\"]`: a pushed version tag is what publishes a release"
        );
        Assert.Contains("gh release create", release, StringComparison.Ordinal);
        foreach (var asset in new[] { "suwpptx-win-x64.zip", "suwpptx-linux-x64.zip", "install.sh" })
        {
            Assert.True(
                release.Contains(asset, StringComparison.Ordinal),
                $"release.yml must attach {asset} to the release (add it to the `gh release create` command)"
            );
        }
        Assert.Contains("contents: write", release, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-2")]
    public void Package_script_puts_the_executable_wwwroot_and_VERSION_at_the_zip_root()
    {
        var script = RepositoryFiles.Read("scripts/.internal/package.sh");

        foreach (
            var rule in new[] { "suwpptx.exe", "EXE=\"suwpptx\"", "--self-contained true", "mv \"$STAGE/$PUBLISHED\" \"$STAGE/$EXE\"" }
        )
        {
            Assert.True(
                script.Contains(rule, StringComparison.Ordinal),
                $"package.sh must contain `{rule}`: install.sh and the start hook expect a self-contained app whose executable is named suwpptx or suwpptx.exe"
            );
        }
        Assert.True(
            script.Contains("(cd \"$STAGE\" && zip -q -r \"$ZIP\" .)", StringComparison.Ordinal)
                && script.Contains("cp -r \"$ROOT/Suwpptx.Web/dist\" \"$STAGE/wwwroot\"", StringComparison.Ordinal)
                && script.Contains("\"$STAGE/VERSION\"", StringComparison.Ordinal),
            "package.sh must stage the app, wwwroot/ and VERSION in one folder and zip that folder's contents, so they sit at the zip root"
        );
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-10")]
    public void Ci_installs_and_starts_the_win_x64_app_on_a_windows_runner_and_checks_the_previews()
    {
        var ci = RepositoryFiles.Read(".github/workflows/ci.yml");
        var windows = WindowsJob().Match(ci);

        Assert.True(windows.Success, "ci.yml must have a job `windows` (two-space indent) that installs the built app on a Windows runner");
        var body = windows.Groups["body"].Value;
        Assert.Contains("runs-on: windows-", body, StringComparison.Ordinal);
        Assert.Contains("shell: bash", body, StringComparison.Ordinal);
        foreach (var step in new[] { "install.sh", "plugin/scripts/start-app.sh", "done.json", "SUWPPTX_APP_ZIP", "suwpptx-win-x64" })
        {
            Assert.True(
                body.Contains(step, StringComparison.Ordinal),
                $"the windows job must use `{step}` (install, start and render the way a user's machine does)"
            );
        }
        Assert.DoesNotContain("poppler", ci, StringComparison.OrdinalIgnoreCase);
    }
}
