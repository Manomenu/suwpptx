using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Previews;

internal sealed partial class PreviewWorker(
    RootFolder root,
    SlideRenderer renderer,
    TimeProvider timeProvider,
    ILogger<PreviewWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private static readonly EnumerationOptions Everything = new() { RecurseSubdirectories = true, IgnoreInaccessible = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RenderWhatIsMissingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            // Why: one bad deck or a folder removed mid-scan must not stop previews for everything else
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                LoopFailed(logger, error);
            }
            await Task.Delay(Interval, timeProvider, stoppingToken);
        }
    }

    private async Task RenderWhatIsMissingAsync(CancellationToken cancellationToken)
    {
        if (!renderer.Available)
        {
            return;
        }
        foreach (var deck in Decks())
        {
            // Why: a deck written a moment ago may still be growing; it is picked up on a later pass
            if (timeProvider.GetUtcNow() - deck.LastWriteTimeUtc < Interval)
            {
                continue;
            }
            var target = PreviewLocation.Of(root, deck);
            if (target is null || PreviewStatus.Of(target, deck, renderer).State != PreviewState.Rendering)
            {
                continue;
            }
            await RenderAsync(deck, target, cancellationToken);
        }
    }

    private List<FileInfo> Decks() =>
        [
            .. new[] { root.Templates, root.Workspaces }
                .Where(Directory.Exists)
                .SelectMany(directory => new DirectoryInfo(directory).EnumerateFiles("*", Everything))
                .Where(file =>
                    file.Extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase)
                    || file.Extension.Equals(".potx", StringComparison.OrdinalIgnoreCase)
                ),
        ];

    private async Task RenderAsync(FileInfo deck, string target, CancellationToken cancellationToken)
    {
        var parent = Path.GetDirectoryName(target) ?? throw new InvalidOperationException($"{target} has no folder");
        Directory.CreateDirectory(parent);
        // Why: the result is built in a hidden sibling and moved into place, so done.json or error.txt never appears half-written
        var temporary = Path.Combine(parent, $".{Path.GetFileName(target)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        try
        {
            var changedAt = deck.LastWriteTimeUtc;
            var name = root.Relative(deck.FullName);
            if (DeckFile.ProblemWith(deck.FullName) is { } problem)
            {
                await File.WriteAllTextAsync(Path.Combine(temporary, PreviewStatus.ErrorFile), problem, cancellationToken);
                Failed(logger, name, problem);
            }
            else
            {
                try
                {
                    var slides = await renderer.RenderAsync(deck.FullName, temporary, cancellationToken);
                    await File.WriteAllTextAsync(
                        Path.Combine(temporary, PreviewStatus.DoneFile),
                        $$"""{"slides": {{slides}}}""",
                        cancellationToken
                    );
                    Rendered(logger, name, slides);
                }
                catch (InvalidOperationException error)
                {
                    await File.WriteAllTextAsync(Path.Combine(temporary, PreviewStatus.ErrorFile), error.Message, cancellationToken);
                    Failed(logger, name, error.Message);
                }
            }
            deck.Refresh();
            if (!deck.Exists || deck.LastWriteTimeUtc != changedAt)
            {
                return;
            }
            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }
            Directory.Move(temporary, target);
        }
        finally
        {
            if (Directory.Exists(temporary))
            {
                Directory.Delete(temporary, recursive: true);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Drew {Slides} slides of {Deck}")]
    private static partial void Rendered(ILogger logger, string deck, int slides);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No slide previews for {Deck}: {Reason}")]
    private static partial void Failed(ILogger logger, string deck, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "The preview pass failed and will be tried again")]
    private static partial void LoopFailed(ILogger logger, Exception error);
}
