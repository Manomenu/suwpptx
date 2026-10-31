using System.Net;

namespace Suwpptx.Server.Tests.Api;

public sealed class PathSandboxTests : IDisposable
{
    private const string Secret = "top secret contents";

    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Theory]
    [Trait("criterion", "AC-pptx-studio-16")]
    [InlineData("../../outside.pptx")]
    [InlineData("workspaces/ws/../../../etc/passwd")]
    [InlineData("/etc/passwd.pptx")]
    [InlineData("workspaces/ws/resources/.hidden.pptx")]
    public async Task A_preview_of_a_path_that_leaves_the_folder_is_refused(string deck)
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");

        using var status = await client.GetAsync($"/api/previews?deck={Uri.EscapeDataString(deck)}", TestContext.Current.CancellationToken);
        using var slide = await client.GetAsync(
            $"/api/previews/slides/1?deck={Uri.EscapeDataString(deck)}",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, status.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, slide.StatusCode);
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-16")]
    public async Task A_download_that_climbs_out_of_the_workspace_serves_nothing()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        await File.WriteAllTextAsync(Path.Combine(app.Root, "secret.txt"), Secret, TestContext.Current.CancellationToken);

        using var response = await client.GetAsync("/api/workspaces/ws/files/..%2F..%2Fsecret.txt", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True((int)response.StatusCode is >= 400 and < 500, $"expected a refusal, got {(int)response.StatusCode}");
        Assert.DoesNotContain(Secret, body, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-16")]
    public async Task A_delete_that_climbs_out_of_the_workspace_deletes_nothing()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        var sibling = Path.Combine(app.Root, "workspaces", "other");
        await File.WriteAllTextAsync(sibling, Secret, TestContext.Current.CancellationToken);

        using var response = await client.DeleteAsync("/api/workspaces/ws/files/..%2Fother", TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True((int)response.StatusCode is >= 400 and < 500, $"expected a refusal, got {(int)response.StatusCode}");
        Assert.True(File.Exists(sibling));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-16")]
    public async Task An_upload_named_with_a_parent_folder_writes_nothing_outside_resources()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");

        using var response = await client.UploadAsync("/api/workspaces/ws/resources/..%2Fx", "payload"u8.ToArray());

        Assert.True((int)response.StatusCode is >= 400 and < 500, $"expected a refusal, got {(int)response.StatusCode}");
        Assert.Empty(Directory.GetFiles(app.Root, "x", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(Path.Combine(app.Root, "workspaces", "ws", "resources")));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-16")]
    public async Task A_template_download_with_a_parent_folder_serves_nothing()
    {
        using var client = app.CreateClient();
        await File.WriteAllTextAsync(Path.Combine(app.Root, "secret.txt"), Secret, TestContext.Current.CancellationToken);

        using var response = await client.GetAsync("/api/templates/..%2Fsecret.txt", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True((int)response.StatusCode is >= 400 and < 500, $"expected a refusal, got {(int)response.StatusCode}");
        Assert.DoesNotContain(Secret, body, StringComparison.Ordinal);
    }
}
