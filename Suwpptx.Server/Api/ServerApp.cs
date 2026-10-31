using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Suwpptx.Server.Changes;
using Suwpptx.Server.Folder;
using Suwpptx.Server.Health;
using Suwpptx.Server.Previews;
using Suwpptx.Server.Templates;
using Suwpptx.Server.Workspaces;

namespace Suwpptx.Server.Api;

internal static class ServerApp
{
    // Why: the OpenAPI build step starts the app only to read its routes, so no background work may run or create the user's folder
    private static bool WritingOpenApiDocument => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddFolder(builder.Configuration);
        builder.Services.AddSingleton<SlideRenderer>();
        builder.Services.AddHostedService<PreviewWorker>();
        builder.Services.AddSingleton<FolderWatch>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<FolderWatch>());
        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier
        );
        builder.Services.AddOpenApi();
        if (WritingOpenApiDocument)
        {
            builder.Services.RemoveAll<IHostedService>();
        }

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMiddleware<LocalOnly>();

        // Why: in development there is no wwwroot and vite serves the UI; the packaged app has one next to the program, not in the current folder
        var webRoot = WebRootWithIndex(Path.Combine(AppContext.BaseDirectory, "wwwroot")) ?? WebRootWithIndex(app.Environment.WebRootPath);
        var web = webRoot is null ? null : new StaticFileOptions { FileProvider = new PhysicalFileProvider(webRoot) };
        if (web is not null)
        {
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = web.FileProvider });
            app.UseStaticFiles(web);
        }
        // Why: with the implicit routing the fallback endpoint would be chosen before the static files get their turn
        app.UseRouting();

        var api = app.MapGroup("/api");
        api.MapHealth();
        api.MapWorkspaces();
        api.MapTemplates();
        api.MapPreviews();
        api.MapChanges();
        if (web is not null)
        {
            // Why: client-side routes such as /workspaces/q3 have no file; /api paths that match nothing stay a 404 problem
            app.MapFallbackToFile("{*path:regex(^(?!api(/|$)).*$)}", "index.html", web);
        }
        return app;
    }

    private static string? WebRootWithIndex(string? folder) =>
        folder is not null && File.Exists(Path.Combine(folder, "index.html")) ? folder : null;
}
