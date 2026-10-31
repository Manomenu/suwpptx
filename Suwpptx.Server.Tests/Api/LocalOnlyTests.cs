using System.Net;
using System.Net.Http.Json;

namespace Suwpptx.Server.Tests.Api;

public sealed class LocalOnlyTests : IDisposable
{
    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    private string Workspace => Path.Combine(app.Root, "workspaces", "ws");

    [Fact]
    [Trait("criterion", "AC-pptx-studio-16")]
    public async Task A_request_for_another_host_is_refused_and_changes_nothing()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/workspaces/ws");
        request.Headers.Host = "evil.example";

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(Directory.Exists(Workspace));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-16")]
    public async Task A_delete_from_another_origin_is_refused_and_the_workspace_stays()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/workspaces/ws");
        request.Headers.Add("Origin", "https://evil.example");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(Directory.Exists(Workspace));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-16")]
    public async Task A_create_from_another_origin_is_refused_and_nothing_is_created()
    {
        using var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/workspaces")
        {
            Content = JsonContent.Create(new { name = "ws" }),
        };
        request.Headers.Add("Origin", "https://evil.example");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(Directory.Exists(Workspace));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-16")]
    public async Task The_same_delete_from_the_apps_own_localhost_origin_goes_through()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/workspaces/ws");
        request.Headers.Add("Origin", "http://localhost:3310");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(Directory.Exists(Workspace));
    }
}
