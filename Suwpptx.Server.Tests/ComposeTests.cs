using System.Text.RegularExpressions;

namespace Suwpptx.Server.Tests;

public sealed partial class ComposeTests
{
    [GeneratedRegex(@"^  ([a-z][a-z0-9-]*):\s*$", RegexOptions.Multiline)]
    private static partial Regex Service();

    [Fact]
    public void Compose_runs_the_server_and_the_web_app_with_the_web_on_localhost_only()
    {
        var compose = RepositoryFiles.Read("compose.yaml");
        var services = Service().Matches(compose).Select(match => match.Groups[1].Value).ToHashSet();

        Assert.True(
            services.SetEquals(["server", "web"]),
            $"compose.yaml must define exactly the services server and web (smoke.sh and `just up` reach the app through web), found: {string.Join(", ", services.Order())}"
        );
        Assert.True(
            compose.Contains("\"127.0.0.1:${WEB_PORT:-8200}:8080\"", StringComparison.Ordinal),
            "publish the web port on localhost only (\"127.0.0.1:${WEB_PORT:-8200}:8080\"): the app has no login"
        );
    }
}
