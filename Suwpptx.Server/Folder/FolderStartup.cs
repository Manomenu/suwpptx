namespace Suwpptx.Server.Folder;

internal sealed partial class FolderStartup(RootFolder root, ILogger<FolderStartup> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        root.Create();
        Using(logger, root.Path);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Workspaces and templates live in {Root}")]
    private static partial void Using(ILogger logger, string root);
}
