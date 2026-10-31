namespace Suwpptx.Server.Folder;

// Why: an uploaded .html or .svg opened directly must not run scripts as the app, so every file but a PDF gets a sandboxed origin
internal sealed class SandboxedFile(IResult file, string contentType) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        if (contentType != "application/pdf")
        {
            httpContext.Response.Headers.ContentSecurityPolicy = "sandbox";
        }
        return file.ExecuteAsync(httpContext);
    }
}
