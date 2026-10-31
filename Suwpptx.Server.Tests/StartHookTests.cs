using System.Net;
using System.Net.Sockets;

namespace Suwpptx.Server.Tests;

public sealed class StartHookTests
{
    private const string FakeApp = """
        #!/usr/bin/env python3
        import http.server, os, urllib.parse
        port = urllib.parse.urlparse(os.environ["ASPNETCORE_URLS"]).port
        root = os.environ["SUWPPTX_ROOT"]
        class Handler(http.server.BaseHTTPRequestHandler):
            def do_GET(self):
                self.send_response(200)
                self.end_headers()
                self.wfile.write(('{"status":"ok","root":"' + root + '"}').encode())
            def log_message(self, *args):
                pass
        http.server.HTTPServer(("127.0.0.1", port), Handler).serve_forever()
        """;

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-6")]
    public async Task When_the_app_is_not_installed_the_hook_prints_one_line_and_returns_fast()
    {
        BashScript.Require("bash", "curl");
        using var scratch = new ScratchFolder();
        var setup = Setup(scratch);

        var result = await Run(setup, TestContext.Current.CancellationToken);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("Suwpptx Studio isn't installed yet: ask Claude to install Manomenu/suwpptx.", result.Output.Trim());
        AssertFast(result);
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-6")]
    public async Task An_installed_app_that_is_not_answering_is_started_once_in_the_session_folder()
    {
        BashScript.Require("bash", "curl", "python3");
        using var scratch = new ScratchFolder();
        var setup = Setup(scratch);
        var cancellationToken = TestContext.Current.CancellationToken;
        var app = Path.Combine(setup.Data, "app", "suwpptx");
        Directory.CreateDirectory(Path.GetDirectoryName(app) ?? setup.Data);
        await File.WriteAllTextAsync(app, FakeApp, cancellationToken);
        BashScript.MakeExecutable(app);
        try
        {
            var first = await Run(setup, cancellationToken);

            Assert.Equal(0, first.ExitCode);
            Assert.Equal($"Suwpptx Studio runs at {setup.Url}", first.Output.Trim());
            AssertFast(first);
            Assert.True(File.Exists(Path.Combine(setup.Data, "app.pid")), "the hook must record the app's PID in <data>/app.pid");
            using var client = new HttpClient();
            var health = await client.GetStringAsync($"{setup.Url}/api/health", cancellationToken);
            Assert.Contains(setup.Project.Replace("\\", "\\\\", StringComparison.Ordinal), health, StringComparison.Ordinal);

            var second = await Run(setup, cancellationToken);

            Assert.Equal(0, second.ExitCode);
            Assert.Equal("", second.Output.Trim());
            AssertFast(second);
        }
        finally
        {
            await Stop(setup, cancellationToken);
        }
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-6")]
    public async Task A_port_held_by_another_program_is_reported_and_no_second_app_is_started()
    {
        BashScript.Require("bash", "curl", "python3");
        using var scratch = new ScratchFolder();
        var setup = Setup(scratch);
        var cancellationToken = TestContext.Current.CancellationToken;
        var app = Path.Combine(setup.Data, "app", "suwpptx");
        Directory.CreateDirectory(Path.GetDirectoryName(app) ?? setup.Data);
        await File.WriteAllTextAsync(app, FakeApp, cancellationToken);
        BashScript.MakeExecutable(app);
        var other = new System.Diagnostics.ProcessStartInfo("python3") { WorkingDirectory = scratch.Path };
        other.ArgumentList.Add("-m");
        other.ArgumentList.Add("http.server");
        other.ArgumentList.Add(setup.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        other.ArgumentList.Add("--bind");
        other.ArgumentList.Add("127.0.0.1");
        other.RedirectStandardError = true;
        other.RedirectStandardOutput = true;
        using var holder = System.Diagnostics.Process.Start(other) ?? throw new InvalidOperationException("python3 did not start");
        try
        {
            await Eventually.TrueAsync(
                () => Listening(setup.Port),
                "the other program listens on the port",
                TimeSpan.FromSeconds(10),
                cancellationToken
            );

            var result = await Run(setup, cancellationToken);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal("Port 6310 is used by another program: close it, then start a new session.", result.Output.Trim());
            Assert.False(File.Exists(Path.Combine(setup.Data, "app.pid")), "the hook must not start a second app while the port is taken");
            AssertFast(result);
        }
        finally
        {
            holder.Kill();
        }
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-6")]
    public void The_plugin_runs_the_start_script_at_session_start()
    {
        var manifest = System.Text.Json.JsonDocument.Parse(RepositoryFiles.Read("plugin/.claude-plugin/plugin.json")).RootElement;

        var command = manifest
            .GetProperty("hooks")
            .GetProperty("SessionStart")[0]
            .GetProperty("hooks")[0]
            .GetProperty("command")
            .GetString();

        Assert.Equal("bash \"${CLAUDE_PLUGIN_ROOT}/scripts/start-app.sh\"", command);
    }

    private static void AssertFast(ScriptResult result) =>
        Assert.True(
            result.Elapsed < TimeSpan.FromSeconds(2),
            $"the start hook must return within 2 s, took {result.Elapsed.TotalSeconds:0.0} s"
        );

    private static bool Listening(int port)
    {
        try
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static HookSetup Setup(ScratchFolder scratch)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var project = scratch.Combine("project");
        Directory.CreateDirectory(project);
        return new HookSetup(scratch.Combine("data"), project, port);
    }

    private static Task<ScriptResult> Run(HookSetup setup, CancellationToken cancellationToken) =>
        BashScript.RunAsync(
            Path.Combine(RepositoryFiles.Root, "plugin", "scripts", "start-app.sh"),
            setup.Project,
            new Dictionary<string, string?>
            {
                ["CLAUDE_PLUGIN_DATA"] = setup.Data,
                ["CLAUDE_PROJECT_DIR"] = setup.Project,
                ["SUWPPTX_URL"] = setup.Url,
                ["SUWPPTX_NO_BROWSER"] = "1",
            },
            cancellationToken
        );

    private static async Task Stop(HookSetup setup, CancellationToken cancellationToken)
    {
        var pidFile = Path.Combine(setup.Data, "app.pid");
        if (File.Exists(pidFile) && int.TryParse((await File.ReadAllTextAsync(pidFile, cancellationToken)).Trim(), out var pid))
        {
            try
            {
                System.Diagnostics.Process.GetProcessById(pid).Kill();
            }
            catch (ArgumentException)
            {
                // Why: the app already ended, which is what this cleanup wants.
            }
        }
    }

    private sealed record HookSetup(string Data, string Project, int Port)
    {
        public string Url => $"http://localhost:{Port}";
    }
}
