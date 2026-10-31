namespace Suwpptx.Server.Api;

// Why: the app has no login, so a page on another site must not be able to change the folder through the user's browser
internal sealed class LocalOnly(RequestDelegate next)
{
    private static readonly string[] LocalHosts = ["localhost", "127.0.0.1", "[::1]"];

    public Task InvokeAsync(HttpContext context)
    {
        if (
            !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method)
            && !HttpMethods.IsOptions(context.Request.Method)
            && FromOtherSite(context.Request)
        )
        {
            return Results
                .Problem(
                    detail: "Requests from other sites are refused: open the app at its own localhost address.",
                    statusCode: StatusCodes.Status403Forbidden
                )
                .ExecuteAsync(context);
        }
        return next(context);
    }

    private static bool FromOtherSite(HttpRequest request) =>
        request.Headers.TryGetValue("Origin", out var origin)
        && !(
            Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var uri)
            && LocalHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase)
        );
}
