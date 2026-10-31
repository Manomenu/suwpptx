using Microsoft.Extensions.Options;

namespace Suwpptx.Server.Database;

internal static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, bool migrateOnStart)
    {
        var databaseOptions = services
            .AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = configuration.GetConnectionString("Default") ?? "")
            .Validate(
                options => options.ConnectionString.Length > 0,
                "ConnectionStrings:Default is missing: set it in user secrets or as ConnectionStrings__Default."
            );
        services.AddSingleton(provider => Pool.Create(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));
        if (migrateOnStart)
        {
            databaseOptions.ValidateOnStart();
            services.AddHostedService<DatabaseStartup>();
        }
        return services;
    }
}
