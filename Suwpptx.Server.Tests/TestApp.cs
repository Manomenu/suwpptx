using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Suwpptx.Server.Tests;

internal sealed class TestApp(long? uploadLimitMegabytes = null, string? webRoot = null) : WebApplicationFactory<Program>
{
    public string Root { get; } = Directory.CreateTempSubdirectory("suwpptx-test-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        if (webRoot is not null)
        {
            builder.UseWebRoot(webRoot);
        }
        var settings = new Dictionary<string, string?> { ["Folder:Root"] = Root };
        if (uploadLimitMegabytes is { } limit)
        {
            settings["Folder:UploadLimitMegabytes"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
