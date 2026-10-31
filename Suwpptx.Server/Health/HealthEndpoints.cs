using Microsoft.AspNetCore.Http.HttpResults;
using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Health;

internal static partial class HealthEndpoints
{
    public static void MapHealth(this IEndpointRouteBuilder routes) => routes.MapGet("/health", Check);

    private static Results<Ok<HealthStatus>, ProblemHttpResult> Check(RootFolder root, ILoggerFactory loggerFactory)
    {
        var missing = new[] { root.Path, root.Templates, root.Workspaces }.FirstOrDefault(folder => !Directory.Exists(folder));
        if (missing is null)
        {
            return TypedResults.Ok(new HealthStatus("ok"));
        }
        FolderMissing(loggerFactory.CreateLogger("Suwpptx.Server.Health"), missing);
        return TypedResults.Problem(
            detail: $"The folder {missing} is missing: create it, or set SUWPPTX_ROOT to a folder you can write.",
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The health check found the folder {Folder} missing")]
    private static partial void FolderMissing(ILogger logger, string folder);
}
