using System.Text.Json;

namespace Suwpptx.Server.Previews;

internal static class PreviewStatus
{
    public const string DoneFile = "done.json";

    public const string ErrorFile = "error.txt";

    public static DeckPreview Of(string previewDirectory, FileInfo deck, SlideRenderer renderer)
    {
        var done = new FileInfo(Path.Combine(previewDirectory, DoneFile));
        var error = new FileInfo(Path.Combine(previewDirectory, ErrorFile));
        // Why: a deck saved again after its pictures were made must be drawn again, so an older marker counts for nothing
        if (done.Exists && done.LastWriteTimeUtc >= deck.LastWriteTimeUtc && Slides(done) is { } slides)
        {
            return new DeckPreview(PreviewState.Ready, slides, null);
        }
        if (error.Exists && error.LastWriteTimeUtc >= deck.LastWriteTimeUtc)
        {
            return new DeckPreview(PreviewState.Failed, 0, File.ReadAllText(error.FullName));
        }
        return renderer.Available
            ? new DeckPreview(PreviewState.Rendering, 0, null)
            : new DeckPreview(PreviewState.Failed, 0, SlideRenderer.Problem);
    }

    private static int? Slides(FileInfo done)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(done.FullName));
            return document.RootElement.GetProperty("slides").GetInt32();
        }
        catch (Exception error)
            when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or IOException)
        {
            return null;
        }
    }
}
