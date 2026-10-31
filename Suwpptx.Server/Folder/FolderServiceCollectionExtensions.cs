namespace Suwpptx.Server.Folder;

internal static class FolderServiceCollectionExtensions
{
    public static IServiceCollection AddFolder(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FolderOptions>()
            .BindConfiguration("Folder")
            .PostConfigure(options =>
            {
                if (options.Root.Length == 0)
                {
                    options.Root =
                        configuration["SUWPPTX_ROOT"]
                        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "suwpptx");
                }
            })
            .Validate(options => options.UploadLimitMegabytes > 0, "Folder:UploadLimitMegabytes must be a positive number of megabytes.");
        services.AddSingleton<RootFolder>();
        services.AddHostedService<FolderStartup>();
        return services;
    }
}
