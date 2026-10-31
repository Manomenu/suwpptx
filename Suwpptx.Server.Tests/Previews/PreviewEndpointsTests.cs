using System.Net;

namespace Suwpptx.Server.Tests.Previews;

public sealed class PreviewEndpointsTests : IDisposable
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Theory]
    [Trait("criterion", "AC-pptx-studio-6")]
    [InlineData("three-slides.pptx", "workspaces/ws/resources/deck.pptx")]
    [InlineData("three-slides.potx", "templates/brand.potx")]
    public async Task A_deck_in_resources_or_templates_gets_three_slide_pictures(string fixture, string deck)
    {
        LibreOffice.RequireInstalled();
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        DeckFixtures.Copy(fixture, Path.Combine(app.Root, deck));
        var url = $"/api/previews?deck={Uri.EscapeDataString(deck)}";

        var status = await Eventually.ReturnsAsync(
            () => client.GetJsonAsync(url),
            preview => preview.GetProperty("state").GetString() != "Rendering",
            $"{deck} has been drawn",
            LibreOffice.RenderTimeout,
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Ready", status.GetProperty("state").GetString());
        Assert.Equal(3, status.GetProperty("slides").GetInt32());
        foreach (var number in new[] { 1, 2, 3 })
        {
            using var slide = await client.GetAsync(
                $"/api/previews/slides/{number}?deck={Uri.EscapeDataString(deck)}",
                TestContext.Current.CancellationToken
            );
            var bytes = await slide.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, slide.StatusCode);
            Assert.Equal("image/png", slide.Content.Headers.ContentType?.MediaType);
            Assert.True(bytes.AsSpan().StartsWith(PngSignature), $"slide {number} is not a PNG");
        }
        using var missing = await client.GetAsync(
            $"/api/previews/slides/4?deck={Uri.EscapeDataString(deck)}",
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-6")]
    public async Task Previews_are_refused_for_files_that_are_not_decks_or_not_in_a_known_place()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        await File.WriteAllTextAsync(
            Path.Combine(app.Root, "workspaces", "ws", "resources", "notes.txt"),
            "x",
            TestContext.Current.CancellationToken
        );
        DeckFixtures.Copy("three-slides.pptx", Path.Combine(app.Root, "stray.pptx"));

        using var notADeck = await client.GetAsync(
            "/api/previews?deck=workspaces/ws/resources/notes.txt",
            TestContext.Current.CancellationToken
        );
        using var strayDeck = await client.GetAsync("/api/previews?deck=stray.pptx", TestContext.Current.CancellationToken);
        using var absent = await client.GetAsync("/api/previews?deck=templates/none.potx", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, notADeck.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, strayDeck.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
    }
}
