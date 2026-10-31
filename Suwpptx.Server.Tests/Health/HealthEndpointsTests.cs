using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Suwpptx.Server.Tests.Health;

public sealed class HealthEndpointsTests : IDisposable
{
    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    public async Task Health_is_ok_when_the_folder_is_there()
    {
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_says_what_to_do_when_the_folder_is_gone_and_names_the_trace_in_the_log()
    {
        using var logs = new CapturedLogs();
        using var watched = app.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<ILoggerProvider>(logs))
        );
        using var client = watched.CreateClient();
        Directory.Delete(app.Root, recursive: true);

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var traceId = JsonDocument.Parse(body).RootElement.GetProperty("traceId").GetString();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("SUWPPTX_ROOT", body, StringComparison.Ordinal);
        Assert.Matches("^[0-9a-f]{32}$", traceId);
        Assert.Contains(logs.Entries, entry => entry.Level == LogLevel.Warning && entry.TraceId == traceId);
    }
}
