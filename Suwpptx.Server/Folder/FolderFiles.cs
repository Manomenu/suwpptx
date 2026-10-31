using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;

namespace Suwpptx.Server.Folder;

internal static class FolderFiles
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private static readonly EnumerationOptions Visible = new() { RecurseSubdirectories = true, IgnoreInaccessible = true };

    public static IReadOnlyList<FolderEntry> List(string directory, string relativeTo) =>
        Directory.Exists(directory)
            ?
            [
                .. new DirectoryInfo(directory)
                    .EnumerateFiles("*", Visible)
                    .Where(file => !Hidden(Path.GetRelativePath(relativeTo, file.FullName)))
                    .Select(file => Entry(file, relativeTo))
                    .OrderBy(entry => entry.Path, StringComparer.Ordinal),
            ]
            : [];

    public static FolderEntry Entry(FileInfo file, string relativeTo) =>
        new(Path.GetRelativePath(relativeTo, file.FullName).Replace('\\', '/'), file.Length, new DateTimeOffset(file.LastWriteTimeUtc));

    // Why: a file is written under a hidden name and renamed when complete, so a half-sent upload never shows up
    public static async Task<Results<Created<FolderEntry>, ProblemHttpResult>> SaveAsync(
        HttpRequest request,
        string target,
        string relativeTo,
        long limit,
        bool replace,
        CancellationToken cancellationToken
    )
    {
        var name = Path.GetFileName(target);
        if (request.ContentLength > limit)
        {
            return TooLarge(limit, Path.GetDirectoryName(target) ?? target);
        }
        if (File.Exists(target) && !replace)
        {
            return TypedResults.Problem(
                detail: $"{name} already exists: send replace=true to replace it, or pick another name.",
                statusCode: StatusCodes.Status409Conflict
            );
        }
        var sizeFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = limit + 1;
        }
        var directory = Path.GetDirectoryName(target) ?? throw new InvalidOperationException($"{target} has no folder");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".upload-{Guid.NewGuid():N}");
        try
        {
            long written = 0;
            await using (var output = File.Create(temporary))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await request.Body.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    written += read;
                    if (written > limit)
                    {
                        return TooLarge(limit, directory);
                    }
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
            File.Move(temporary, target, overwrite: true);
            var entry = Entry(new FileInfo(target), relativeTo);
            return TypedResults.Created((string?)null, entry);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    public static IResult Download(string path, bool asAttachment)
    {
        if (!File.Exists(path))
        {
            return TypedResults.Problem(
                detail: $"{Path.GetFileName(path)} doesn't exist (any more).",
                statusCode: StatusCodes.Status404NotFound
            );
        }
        if (!ContentTypes.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return new SandboxedFile(
            TypedResults.PhysicalFile(path, contentType, asAttachment ? Path.GetFileName(path) : null, enableRangeProcessing: true),
            contentType
        );
    }

    public static bool Hidden(string relativePath) => relativePath.Replace('\\', '/').Split('/').Any(segment => segment.StartsWith('.'));

    private static ProblemHttpResult TooLarge(long limit, string directory) =>
        TypedResults.Problem(
            detail: $"A file can be at most {limit / (1024 * 1024)} MB here. Copy a bigger file straight into {directory} instead.",
            statusCode: StatusCodes.Status413PayloadTooLarge
        );
}
