using System.IO.Compression;
using System.Text.Json;

namespace Suwpptx.Server.Tests;

public sealed class InstallScriptTests
{
    [Fact]
    [Trait("criterion", "AC-windows-compatibility-7")]
    public async Task The_plugin_is_enabled_in_the_root_folder_and_in_no_other_folder()
    {
        BashScript.Require("bash", "claude", "unzip");
        using var scratch = new ScratchFolder();
        var home = Prepare(scratch);
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

    private static Home Prepare(ScratchFolder scratch)
    {
        var path = scratch.Combine("home");
        Directory.CreateDirectory(path);
        BuildAppZip(scratch.Combine("app.zip"));
        return new Home(path);
    }

    private static Task<ScriptResult> Install(Home home, ScratchFolder scratch, CancellationToken cancellationToken) =>
        BashScript.RunAsync(
            Path.Combine(RepositoryFiles.Root, "install.sh"),
            scratch.Path,
            new Dictionary<string, string?>
            {
                ["HOME"] = home.Path,
                ["CLAUDE_CONFIG_DIR"] = home.Config,
                ["SUWPPTX_ROOT"] = null,
                ["SUWPPTX_MARKETPLACE"] = RepositoryFiles.Root,
                ["SUWPPTX_APP_ZIP"] = scratch.Combine("app.zip"),
                ["SUWPPTX_NO_SHORTCUT"] = "1",
            },
            cancellationToken
        );

    private static void BuildAppZip(string zipPath)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddEntry(archive, "suwpptx", "#!/bin/sh\nexit 0\n", executable: true);
        AddEntry(archive, "VERSION", "1.0\n", executable: false);
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
    }
}
