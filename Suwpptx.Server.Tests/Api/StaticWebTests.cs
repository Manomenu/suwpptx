using System.Net;
using System.Text.Json;

namespace Suwpptx.Server.Tests.Api;

public sealed class StaticWebTests : IDisposable
{
    private readonly string webRoot = Directory.CreateTempSubdirectory("suwpptx-web-").FullName;

    public StaticWebTests()
    {
        Directory.CreateDirectory(Path.Combine(webRoot, "assets"));
        File.WriteAllText(Path.Combine(webRoot, "index.html"), "<html>the studio</html>");
        File.WriteAllText(Path.Combine(webRoot, "assets", "x.js"), "console.log('x');");
    }

    public void Dispose() => Directory.Delete(webRoot, recursive: true);

    [Theory]
    [Trait("criterion", "AC-windows-compatibility-2")]
    [InlineData("/")]
    [InlineData("/workspaces/q3")]
    [InlineData("/templates")]
    public async Task A_page_address_serves_the_index_so_the_web_app_can_route_it(string address)
    {
        using var app = new TestApp(webRoot: webRoot);
        using var client = app.CreateClient();

        using var response = await client.GetAsync(address, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("<html>the studio</html>", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-2")]
    public async Task A_file_of_the_web_app_is_served_as_it_is()
    {
        using var app = new TestApp(webRoot: webRoot);
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/assets/x.js", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("console.log('x');", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-2")]
    public async Task The_api_still_answers_with_json_and_an_unknown_api_address_is_a_404_problem_not_the_index()
    {
        using var app = new TestApp(webRoot: webRoot);
        using var client = app.CreateClient();

        var health = await client.GetJsonAsync("/api/health");
        using var missing = await client.GetAsync("/api/nope", TestContext.Current.CancellationToken);

        Assert.Equal(JsonValueKind.Object, health.ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-2")]
    public async Task Without_a_web_app_next_to_the_program_the_root_is_a_404()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
