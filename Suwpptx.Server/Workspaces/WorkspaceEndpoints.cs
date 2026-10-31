using Microsoft.AspNetCore.Http.HttpResults;
using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Workspaces;

internal static class WorkspaceEndpoints
{
    public static void MapWorkspaces(this IEndpointRouteBuilder routes)
    {
        var workspaces = routes.MapGroup("/workspaces");
        workspaces.MapGet("/", List);
        workspaces.MapPost("/", Create);
        workspaces.MapGet("/{name}", Get);
        workspaces.MapPatch("/{name}", Rename);
        workspaces.MapDelete("/{name}", Delete);
        workspaces.MapPut("/{name}/resources/{fileName}", Upload);
        workspaces.MapGet("/{name}/files/{**path}", Download);
        workspaces.MapDelete("/{name}/files/{**path}", DeleteFile);
    }

    private static IReadOnlyList<WorkspaceSummary> List(RootFolder root) =>
        [
            .. new DirectoryInfo(root.Workspaces)
                .EnumerateDirectories()
                .Where(directory => FolderNames.ProblemWith(directory.Name) is null)
                .Select(directory => WorkspaceFolder.Summary(directory.FullName))
                .OrderByDescending(summary => summary.ChangedAt),
        ];

    private static Results<Created<WorkspaceSummary>, ProblemHttpResult> Create(WorkspaceName request, RootFolder root)
    {
        if (FolderNames.ProblemWith(request.Name) is { } problem)
        {
            return Unprocessable(problem);
        }
        var name = request.Name ?? "";
        var directory = Path.Combine(root.Workspaces, name);
        if (Directory.Exists(directory))
        {
            return Conflict(name);
        }
        WorkspaceFolder.Create(directory);
        return TypedResults.Created($"/api/workspaces/{Uri.EscapeDataString(name)}", WorkspaceFolder.Summary(directory));
    }

    private static Results<Ok<Workspace>, ProblemHttpResult> Get(string name, RootFolder root)
    {
        if (Existing(root, name) is not { } directory)
        {
            return NotFound(name);
        }
        return TypedResults.Ok(new Workspace(name, directory, WorkspaceFolder.Files(directory), WorkspaceFolder.Versions(directory)));
    }

    private static Results<Ok<WorkspaceSummary>, ProblemHttpResult> Rename(string name, WorkspaceName request, RootFolder root)
    {
        if (Existing(root, name) is not { } directory)
        {
            return NotFound(name);
        }
        if (FolderNames.ProblemWith(request.Name) is { } problem)
        {
            return Unprocessable(problem);
        }
        var newName = request.Name ?? "";
        var target = Path.Combine(root.Workspaces, newName);
        if (Directory.Exists(target))
        {
            return Conflict(newName);
        }
        Directory.Move(directory, target);
        return TypedResults.Ok(WorkspaceFolder.Summary(target));
    }

    private static Results<NoContent, ProblemHttpResult> Delete(string name, RootFolder root)
    {
        if (Existing(root, name) is not { } directory)
        {
            return NotFound(name);
        }
        Directory.Delete(directory, recursive: true);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Created<FolderEntry>, ProblemHttpResult>> Upload(
        string name,
        string fileName,
        bool? replace,
        HttpRequest request,
        RootFolder root,
        CancellationToken cancellationToken
    )
    {
        if (Existing(root, name) is not { } directory)
        {
            return NotFound(name);
        }
        if (FolderNames.ProblemWith(fileName) is { } problem)
        {
            return Unprocessable(problem);
        }
        return await FolderFiles.SaveAsync(
            request,
            Path.Combine(directory, "resources", fileName),
            directory,
            root.UploadLimitBytes,
            replace ?? false,
            cancellationToken
        );
    }

    private static IResult Download(string name, string path, bool? download, RootFolder root)
    {
        if (Existing(root, name) is not { } directory)
        {
            return NotFound(name);
        }
        return RootFolder.Inside(directory, path) is { } file ? FolderFiles.Download(file, download ?? false) : OutsideTheWorkspace();
    }

    private static Results<NoContent, ProblemHttpResult> DeleteFile(string name, string path, RootFolder root)
    {
        if (Existing(root, name) is not { } directory)
        {
            return NotFound(name);
        }
        if (RootFolder.Inside(directory, path) is not { } file)
        {
            return OutsideTheWorkspace();
        }
        if (!File.Exists(file))
        {
            return TypedResults.Problem(detail: $"{path} doesn't exist in {name} (any more).", statusCode: StatusCodes.Status404NotFound);
        }
        File.Delete(file);
        return TypedResults.NoContent();
    }

    private static string? Existing(RootFolder root, string name) =>
        root.Workspace(name) is { } directory && Directory.Exists(directory) ? directory : null;

    private static ProblemHttpResult NotFound(string name) =>
        TypedResults.Problem(
            detail: $"There is no workspace named {name}: pick one from the list.",
            statusCode: StatusCodes.Status404NotFound
        );

    private static ProblemHttpResult Conflict(string name) =>
        TypedResults.Problem(detail: $"A workspace named {name} exists: pick another name.", statusCode: StatusCodes.Status409Conflict);

    private static ProblemHttpResult Unprocessable(string problem) =>
        TypedResults.Problem(detail: problem, statusCode: StatusCodes.Status422UnprocessableEntity);

    private static ProblemHttpResult OutsideTheWorkspace() =>
        TypedResults.Problem(
            detail: "Name a file inside the workspace: no '..', no names that start with a dot, no absolute paths.",
            statusCode: StatusCodes.Status400BadRequest
        );
}
