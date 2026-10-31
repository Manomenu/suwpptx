using Microsoft.AspNetCore.Http.HttpResults;
using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Templates;

internal static class TemplateEndpoints
{
    public static void MapTemplates(this IEndpointRouteBuilder routes)
    {
        var templates = routes.MapGroup("/templates");
        templates.MapGet("/", List);
        templates.MapPut("/{fileName}", Upload);
        templates.MapGet("/{fileName}", Download);
        templates.MapDelete("/{fileName}", Delete);
    }

    private static IReadOnlyList<FolderEntry> List(RootFolder root) =>
        [.. FolderFiles.List(root.Templates, root.Templates).Where(entry => !entry.Path.Contains('/', StringComparison.Ordinal))];

    private static async Task<Results<Created<FolderEntry>, ProblemHttpResult>> Upload(
        string fileName,
        bool? replace,
        HttpRequest request,
        RootFolder root,
        CancellationToken cancellationToken
    )
    {
        if (FolderNames.ProblemWith(fileName) is { } problem)
        {
            return Refuse(problem, StatusCodes.Status422UnprocessableEntity);
        }
        if (
            !fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".potx", StringComparison.OrdinalIgnoreCase)
        )
        {
            return Refuse(
                "Templates are .pptx or .potx decks: put other files into a workspace.",
                StatusCodes.Status422UnprocessableEntity
            );
        }
        return await FolderFiles.SaveAsync(
            request,
            Path.Combine(root.Templates, fileName),
            root.Templates,
            root.UploadLimitBytes,
            replace ?? false,
            cancellationToken
        );
    }

    private static IResult Download(string fileName, bool? download, RootFolder root) =>
        FolderNames.ProblemWith(fileName) is null
            ? FolderFiles.Download(Path.Combine(root.Templates, fileName), download ?? false)
            : Refuse("Name a template from the list.", StatusCodes.Status400BadRequest);

    private static Results<NoContent, ProblemHttpResult> Delete(string fileName, RootFolder root)
    {
        if (FolderNames.ProblemWith(fileName) is not null)
        {
            return Refuse("Name a template from the list.", StatusCodes.Status400BadRequest);
        }
        var file = Path.Combine(root.Templates, fileName);
        if (!File.Exists(file))
        {
            return Refuse($"There is no template named {fileName}: pick one from the list.", StatusCodes.Status404NotFound);
        }
        File.Delete(file);
        return TypedResults.NoContent();
    }

    private static ProblemHttpResult Refuse(string detail, int status) => TypedResults.Problem(detail: detail, statusCode: status);
}
