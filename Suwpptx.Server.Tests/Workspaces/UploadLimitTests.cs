using System.Net;
using System.Security.Cryptography;

namespace Suwpptx.Server.Tests.Workspaces;

public sealed class UploadLimitTests : IDisposable
{
    private readonly TestApp app = new(uploadLimitMegabytes: 1);

    public void Dispose() => app.Dispose();

    [Fact]
    [Trait("criterion", "AC-pptx-studio-4")]
    public async Task A_2_MB_file_over_a_1_MB_limit_is_refused_naming_the_limit_and_nothing_is_stored()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");

        using var response = await client.UploadAsync(
            "/api/workspaces/ws/resources/big.bin",
            RandomNumberGenerator.GetBytes(2 * 1024 * 1024)
        );

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("1 MB", await response.DetailAsync(), StringComparison.Ordinal);
        Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(app.Root, "workspaces", "ws", "resources")));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-4")]
    public async Task A_file_sent_without_a_length_is_cut_off_at_the_limit_and_leaves_nothing_behind()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        using var content = new ChunkedContent(RandomNumberGenerator.GetBytes(2 * 1024 * 1024));

        using var response = await client.PutAsync("/api/workspaces/ws/resources/big.bin", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(app.Root, "workspaces", "ws", "resources")));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-4")]
    public async Task A_file_within_the_limit_is_still_accepted()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");

        using var response = await client.UploadAsync("/api/workspaces/ws/resources/small.bin", RandomNumberGenerator.GetBytes(512 * 1024));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
