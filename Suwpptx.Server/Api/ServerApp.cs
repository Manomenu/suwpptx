using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        var api = app.MapGroup("/api");
        api.MapHealth();
        api.MapWorkspaces();
        api.MapTemplates();
        api.MapPreviews();
        api.MapChanges();
        return app;
    }
}
