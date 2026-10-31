using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Previews;

internal static class PreviewEndpoints
{
    public static void MapPreviews(this IEndpointRouteBuilder routes)
    {
        var previews = routes.MapGroup("/previews");
        previews.MapGet("/", Status);
        previews.MapGet("/slides/{number:int}", Slide);
    }

    private static Results<Ok<DeckPreview>, ProblemHttpResult> Status(string? deck, RootFolder root, SlideRenderer renderer) =>
        TryFind(deck, root, out var found, out var problem)
            ? TypedResults.Ok(PreviewStatus.Of(found.Directory, found.Deck, renderer))
            : problem;

    private static Results<PhysicalFileHttpResult, ProblemHttpResult> Slide(int number, string? deck, RootFolder root)
    {
        if (!TryFind(deck, root, out var found, out var problem))
        {
            return problem;
        }
        var picture = Path.Combine(found.Directory, $"{number:D2}.png");
        return File.Exists(picture)
            ? TypedResults.PhysicalFile(picture, "image/png")
            : TypedResults.Problem(
                detail: $"Slide {number} has no picture (yet): ask for the previews first.",
                statusCode: StatusCodes.Status404NotFound
            );
    }

    private static bool TryFind(
        string? deck,
        RootFolder root,
        [NotNullWhen(true)] out Found? found,
        [NotNullWhen(false)] out ProblemHttpResult? problem
    )
    {
        found = null;
        problem = null;
        if (deck is null || RootFolder.Inside(root.Path, deck) is not { } path)
        {
            problem = Refuse(
                "Name a deck inside the folder: no '..', no names that start with a dot, no absolute paths.",
                StatusCodes.Status400BadRequest
            );
            return false;
        }
        if (!path.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".potx", StringComparison.OrdinalIgnoreCase))
        {
            problem = Refuse("Previews are for .pptx and .potx decks.", StatusCodes.Status422UnprocessableEntity);
            return false;
        }
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            problem = Refuse($"{deck} doesn't exist (any more).", StatusCodes.Status404NotFound);
            return false;
        }
        if (PreviewLocation.Of(root, file) is not { } directory)
        {
            problem = Refuse(
                "Previews exist for templates, resources and versions of a workspace.",
                StatusCodes.Status422UnprocessableEntity
            );
            return false;
        }
        found = new Found(file, directory);
        return true;
    }

    private static ProblemHttpResult Refuse(string detail, int status) => TypedResults.Problem(detail: detail, statusCode: status);

    private sealed record Found(FileInfo Deck, string Directory);
}
