using System.Net;
using System.Security.Cryptography;

namespace Suwpptx.Server.Tests.Workspaces;

public sealed class WorkspaceUploadTests : IDisposable
{
    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    [Trait("criterion", "AC-pptx-studio-3")]
    public async Task A_5_MB_file_of_any_type_is_listed_with_its_exact_size_and_downloads_with_the_same_bytes()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        var original = RandomNumberGenerator.GetBytes(5 * 1024 * 1024);

        using var uploaded = await client.UploadAsync("/api/workspaces/ws/resources/scene.blend", original);
        var workspace = await client.GetJsonAsync("/api/workspaces/ws");
        using var downloaded = await client.GetAsync(
            "/api/workspaces/ws/files/resources/scene.blend",
            TestContext.Current.CancellationToken
        );
        var bytes = await downloaded.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var entry = Assert.Single(
            workspace.GetProperty("files").EnumerateArray(),
            file => file.GetProperty("path").GetString() == "resources/scene.blend"
        );
        Assert.Equal(original.Length, entry.GetProperty("size").GetInt64());
        Assert.Equal(HttpStatusCode.OK, downloaded.StatusCode);
        Assert.Equal(SHA256.HashData(original), SHA256.HashData(bytes));
        Assert.Empty(Directory.GetFiles(Path.Combine(app.Root, "workspaces", "ws", "resources"), ".upload-*"));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-3")]
    public async Task Uploading_the_same_name_again_needs_replace_and_then_stores_the_new_bytes()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        var target = Path.Combine(app.Root, "workspaces", "ws", "resources", "notes.txt");
        using var first = await client.UploadAsync("/api/workspaces/ws/resources/notes.txt", "first"u8.ToArray());

        using var refused = await client.UploadAsync("/api/workspaces/ws/resources/notes.txt", "second"u8.ToArray());
        var keptAfterRefusal = await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken);
        using var replaced = await client.UploadAsync("/api/workspaces/ws/resources/notes.txt?replace=true", "second"u8.ToArray());

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("replace=true", await refused.DetailAsync(), StringComparison.Ordinal);
        Assert.Equal("first", keptAfterRefusal);
        Assert.Equal(HttpStatusCode.Created, replaced.StatusCode);
        Assert.Equal("second", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
    }
}
