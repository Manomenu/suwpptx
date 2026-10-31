using System.IO.Compression;
using System.Net;

namespace Suwpptx.Server.Tests.Workspaces;

public sealed class VersionFilesTests : IDisposable
{
    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    [Trait("criterion", "AC-pptx-studio-11")]
    public async Task Versions_are_listed_newest_first_and_the_newest_downloads_as_a_real_pptx_attachment()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        var versions = Path.Combine(app.Root, "workspaces", "ws", "versions");
        DeckFixtures.Copy("three-slides.pptx", Path.Combine(versions, "v1.pptx"));
        DeckFixtures.Copy("three-slides.pptx", Path.Combine(versions, "v2.pptx"));
        var expected = await File.ReadAllBytesAsync(Path.Combine(versions, "v2.pptx"), TestContext.Current.CancellationToken);

        var workspace = await client.GetJsonAsync("/api/workspaces/ws");
        using var download = await client.GetAsync(
            "/api/workspaces/ws/files/versions/v2.pptx?download=true",
            TestContext.Current.CancellationToken
        );
        var bytes = await download.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal([2, 1], workspace.GetProperty("versions").EnumerateArray().Select(item => item.GetProperty("number").GetInt32()));
        Assert.Equal("versions/v2.pptx", workspace.GetProperty("versions")[0].GetProperty("path").GetString());
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("v2.pptx", download.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal(expected, bytes);
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("ppt/presentation.xml"));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-11")]
    public async Task Files_that_are_not_numbered_versions_are_not_listed_as_versions()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        var versions = Path.Combine(app.Root, "workspaces", "ws", "versions");
        DeckFixtures.Copy("three-slides.pptx", Path.Combine(versions, "v10.pptx"));
        DeckFixtures.Copy("three-slides.pptx", Path.Combine(versions, "v9.pptx"));
        DeckFixtures.Copy("three-slides.pptx", Path.Combine(versions, "draft.pptx"));

        var workspace = await client.GetJsonAsync("/api/workspaces/ws");

        Assert.Equal([10, 9], workspace.GetProperty("versions").EnumerateArray().Select(item => item.GetProperty("number").GetInt32()));
    }
}
