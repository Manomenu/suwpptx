using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Suwpptx.Server.Tests;

public sealed partial class InstallScriptTests
{
    [GeneratedRegex(@"^  - (LibreOffice|Python|python-pptx)\b", RegexOptions.Multiline)]
    private static partial Regex MissingTool();

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-4")]
    public async Task Install_in_a_fresh_home_sets_up_the_folder_the_plugin_and_the_app_and_names_missing_tools()
    {
        BashScript.Require("bash", "claude", "unzip");
        using var scratch = new ScratchFolder();
        var home = Prepare(scratch, "1.0");

        var result = await Install(home, scratch, TestContext.Current.CancellationToken);

        Assert.True(result.ExitCode is 0 or 3, $"install.sh must exit 0 (ready) or 3 (needs tools), got {result.ExitCode}: {result.All}");
        Assert.True(Directory.Exists(Path.Combine(home.Root, "templates")), "install.sh must create <root>/templates");
        Assert.True(Directory.Exists(Path.Combine(home.Root, "workspaces")), "install.sh must create <root>/workspaces");
        var settings = JsonDocument
            .Parse(await File.ReadAllTextAsync(Path.Combine(home.Root, ".claude", "settings.json"), TestContext.Current.CancellationToken))
            .RootElement;
        Assert.True(
            settings.GetProperty("enabledPlugins").GetProperty("suwpptx@suwpptx").GetBoolean(),
            "install the plugin at project scope in the root so .claude/settings.json enables suwpptx@suwpptx"
        );
        Assert.True(File.Exists(Path.Combine(home.AppFolder, "suwpptx")), "unpack the app into <config>/plugins/data/suwpptx-suwpptx/app");
        Assert.True(File.Exists(Path.Combine(home.AppFolder, "wwwroot", "index.html")), "unpack wwwroot/ next to the executable");
        if (result.ExitCode == 3)
        {
            Assert.Contains("needs these tools", result.Output, StringComparison.Ordinal);
            var bullets = result.Output.Split('\n').Where(line => line.StartsWith("  - ", StringComparison.Ordinal)).ToList();
            Assert.NotEmpty(bullets);
            Assert.True(
                bullets.All(line => MissingTool().IsMatch(line)),
                $"every missing tool is listed as '  - <LibreOffice|Python|python-pptx> (why): <command>', got: {string.Join(" / ", bullets)}"
            );
            Assert.True(
                bullets.All(line => line.Contains(": ", StringComparison.Ordinal)),
                "every missing tool needs its install command after ': '"
            );
        }
        else
        {
            Assert.Contains("Everything is ready", result.Output, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-5")]
    public async Task A_second_install_changes_nothing_current_and_a_newer_app_replaces_the_old_one_without_touching_the_folders()
    {
        BashScript.Require("bash", "claude", "unzip");
        using var scratch = new ScratchFolder();
        var home = Prepare(scratch, "1.0");
        var cancellationToken = TestContext.Current.CancellationToken;
        await Install(home, scratch, cancellationToken);
        var own = new[] { Path.Combine(home.Root, "workspaces", "q3", "context.md"), Path.Combine(home.Root, "templates", "acme.potx") };
        foreach (var file in own)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? home.Root);
            await File.WriteAllBytesAsync(file, [1, 2, 3, 4, 5], cancellationToken);
        }
        var marker = Path.Combine(home.AppFolder, "marker.txt");
        await File.WriteAllTextAsync(marker, "installed by test", cancellationToken);

        var again = await Install(home, scratch, cancellationToken);

        Assert.True(again.ExitCode is 0 or 3, $"the second install.sh run failed: {again.All}");
        Assert.True(File.Exists(marker), "a current app (same VERSION) must not be unpacked again: compare the VERSION first");
        foreach (var file in own)
        {
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, await File.ReadAllBytesAsync(file, cancellationToken));
        }

        var newer = Path.Combine(scratch.Path, "app-2.0.zip");
        BuildAppZip(newer, "2.0");
        var upgrade = await Install(home, scratch, cancellationToken, newer);

        Assert.True(upgrade.ExitCode is 0 or 3, $"the upgrade run failed: {upgrade.All}");
        Assert.False(File.Exists(marker), "a zip with another VERSION must replace the app folder");
        Assert.Equal("2.0", (await File.ReadAllTextAsync(Path.Combine(home.AppFolder, "VERSION"), cancellationToken)).Trim());
        foreach (var file in own)
        {
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, await File.ReadAllBytesAsync(file, cancellationToken));
        }
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-7")]
    public async Task The_plugin_is_enabled_in_the_root_folder_and_in_no_other_folder()
    {
        BashScript.Require("bash", "claude", "unzip");
        using var scratch = new ScratchFolder();
        var home = Prepare(scratch, "1.0");
        var cancellationToken = TestContext.Current.CancellationToken;
        await Install(home, scratch, cancellationToken);
        var elsewhere = scratch.Combine("elsewhere");
        Directory.CreateDirectory(elsewhere);

        var inRoot = await ListPlugins(home, home.Root, cancellationToken);
        var outside = await ListPlugins(home, elsewhere, cancellationToken);

        Assert.True(inRoot, "a session in the root folder must have suwpptx@suwpptx enabled: install it with --scope project in the root");
        Assert.False(
            outside,
            "a session outside the root folder must not have suwpptx@suwpptx enabled: install it with --scope project, not user"
        );
    }

    private static async Task<bool> ListPlugins(Home home, string folder, CancellationToken cancellationToken)
    {
        var info = new System.Diagnostics.ProcessStartInfo("claude")
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("plugin");
        info.ArgumentList.Add("list");
        info.ArgumentList.Add("--json");
        info.Environment["HOME"] = home.Path;
        info.Environment["CLAUDE_CONFIG_DIR"] = home.Config;
        using var process = System.Diagnostics.Process.Start(info) ?? throw new InvalidOperationException("claude did not start");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errors = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        Assert.True(process.ExitCode == 0, $"claude plugin list --json failed: {await errors}");
        var plugins = JsonDocument.Parse(await output).RootElement.EnumerateArray();
        return plugins.Any(plugin =>
            plugin.GetProperty("id").GetString() == "suwpptx@suwpptx" && plugin.GetProperty("enabled").GetBoolean()
        );
    }

    private static Home Prepare(ScratchFolder scratch, string version)
    {
        var path = scratch.Combine("home");
        Directory.CreateDirectory(path);
        BuildAppZip(scratch.Combine("app.zip"), version);
        return new Home(path);
    }

    private static Task<ScriptResult> Install(Home home, ScratchFolder scratch, CancellationToken cancellationToken, string? zip = null) =>
        BashScript.RunAsync(
            Path.Combine(RepositoryFiles.Root, "install.sh"),
            scratch.Path,
            new Dictionary<string, string?>
            {
                ["HOME"] = home.Path,
                ["CLAUDE_CONFIG_DIR"] = home.Config,
                ["SUWPPTX_ROOT"] = null,
                ["SUWPPTX_MARKETPLACE"] = RepositoryFiles.Root,
                ["SUWPPTX_APP_ZIP"] = zip ?? scratch.Combine("app.zip"),
                ["SUWPPTX_NO_SHORTCUT"] = "1",
            },
            cancellationToken
        );

    private static void BuildAppZip(string zipPath, string version)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddEntry(archive, "suwpptx", "#!/bin/sh\nexit 0\n", executable: true);
        AddEntry(archive, "VERSION", version + "\n", executable: false);
        AddEntry(archive, "wwwroot/index.html", "<!doctype html><title>fake</title>", executable: false);
    }

    private static void AddEntry(ZipArchive archive, string name, string content, bool executable)
    {
        var entry = archive.CreateEntry(name);
        // Why: unzip restores the Unix mode from the high 16 bits of the external attributes.
        entry.ExternalAttributes = (executable ? 0x81ED : 0x81A4) << 16;
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private sealed record Home(string Path)
    {
        public string Root => System.IO.Path.Combine(Path, "suwpptx");

        public string Config => System.IO.Path.Combine(Path, ".claude");

        public string AppFolder => System.IO.Path.Combine(Config, "plugins", "data", "suwpptx-suwpptx", "app");
    }
}
