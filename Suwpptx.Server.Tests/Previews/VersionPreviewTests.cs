using System.Text.Json;

namespace Suwpptx.Server.Tests.Previews;

public sealed class VersionPreviewTests : IDisposable
{
    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    private string Versions => Path.Combine(app.Root, "workspaces", "ws", "versions");

    [Fact]
    [Trait("criterion", "AC-pptx-studio-14")]
    public async Task A_new_version_is_drawn_next_to_it_without_being_asked()
    {
        LibreOffice.RequireInstalled();
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        var pictures = Path.Combine(Versions, "v1-previews");

        DeckFixtures.Copy("three-slides.pptx", Path.Combine(Versions, "v1.pptx"));

        await Eventually.TrueAsync(
            () => File.Exists(Path.Combine(pictures, "done.json")),
            "v1-previews/done.json exists",
            LibreOffice.RenderTimeout,
            TestContext.Current.CancellationToken
        );
        foreach (var picture in new[] { "01.png", "02.png", "03.png" })
        {
            Assert.True(new FileInfo(Path.Combine(pictures, picture)).Length > 0, $"{picture} is missing or empty");
        }
        Assert.Equal(
            3,
            JsonDocument
                .Parse(await File.ReadAllTextAsync(Path.Combine(pictures, "done.json"), TestContext.Current.CancellationToken))
                .RootElement.GetProperty("slides")
                .GetInt32()
        );
        var status = await client.GetJsonAsync("/api/previews?deck=workspaces/ws/versions/v1.pptx");
        Assert.Equal("Ready", status.GetProperty("state").GetString());
        Assert.Equal(3, status.GetProperty("slides").GetInt32());
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-15")]
    public async Task A_version_that_is_not_a_PowerPoint_file_gets_an_error_text_and_no_pictures()
    {
        LibreOffice.RequireInstalled();
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");

        await File.WriteAllTextAsync(Path.Combine(Versions, "v2.pptx"), "this is just text", TestContext.Current.CancellationToken);

        var status = await FailedStatusAsync(client);
        var error = await File.ReadAllTextAsync(Path.Combine(Versions, "v2-previews", "error.txt"), TestContext.Current.CancellationToken);
        Assert.Contains("is not a PowerPoint file", error, StringComparison.Ordinal);
        Assert.Equal(error, status.GetProperty("problem").GetString());
        Assert.Equal(0, status.GetProperty("slides").GetInt32());
        Assert.Empty(Directory.GetFiles(Path.Combine(Versions, "v2-previews"), "*.png"));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-15")]
    public async Task A_password_protected_version_gets_an_error_text_that_says_so()
    {
        LibreOffice.RequireInstalled();
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        byte[] oleSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

        File.WriteAllBytes(Path.Combine(Versions, "v2.pptx"), [.. oleSignature, .. new byte[64]]);

        var status = await FailedStatusAsync(client);
        var error = await File.ReadAllTextAsync(Path.Combine(Versions, "v2-previews", "error.txt"), TestContext.Current.CancellationToken);
        Assert.Contains("password-protected", error, StringComparison.Ordinal);
        Assert.Contains("password-protected", status.GetProperty("problem").GetString(), StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(Path.Combine(Versions, "v2-previews"), "*.png"));
    }

    private static async Task<JsonElement> FailedStatusAsync(HttpClient client)
    {
        var status = await Eventually.ReturnsAsync(
            () => client.GetJsonAsync("/api/previews?deck=workspaces/ws/versions/v2.pptx"),
            preview => preview.GetProperty("state").GetString() != "Rendering",
            "v2.pptx has been judged",
            LibreOffice.RenderTimeout,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("Failed", status.GetProperty("state").GetString());
        return status;
    }
}
