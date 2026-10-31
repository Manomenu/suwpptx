using System.Net;
using System.Net.Http.Json;

namespace Suwpptx.Server.Tests.Workspaces;

public sealed class WindowsNamesTests : IDisposable
{
    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Theory]
    [Trait("criterion", "AC-windows-compatibility-9")]
    [InlineData("CON")]
    [InlineData("nul.txt")]
    [InlineData("Com3.pptx")]
    [InlineData("lpt9")]
    [InlineData("aux.tar.gz")]
    [InlineData("notes.")]
    [InlineData("notes ")]
    public async Task A_workspace_name_that_Windows_reserves_or_cannot_end_with_is_refused_with_422(string name)
    {
        using var client = app.CreateClient();

        using var response = await client.CreateWorkspaceAsync(name);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("Pick another name", await response.DetailAsync(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-9")]
    public async Task The_refusal_says_which_name_Windows_reserves()
    {
        using var client = app.CreateClient();

        using var response = await client.CreateWorkspaceAsync("Com3.pptx");

        Assert.Contains("Windows reserves COM3", await response.DetailAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [Trait("criterion", "AC-windows-compatibility-9")]
    [InlineData("COM1", "COMET", "COM10.txt")]
    public async Task Names_that_only_start_like_a_reserved_name_are_accepted(string first, string second, string third)
    {
        using var client = app.CreateClient();

        foreach (var name in new[] { second, third, "console", first + "x" })
        {
            using var response = await client.CreateWorkspaceAsync(name);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-9")]
    public async Task A_resource_file_and_a_template_with_a_reserved_name_are_refused_with_422()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");

        using var resource = await client.UploadAsync("/api/workspaces/ws/resources/nul.txt", [1]);
        using var template = await client.UploadAsync("/api/templates/Aux.pptx", [1]);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resource.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, template.StatusCode);
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-9")]
    public async Task A_workspace_named_like_an_existing_one_in_another_letter_case_is_refused_with_409()
    {
        using var client = app.CreateClient();
        using var first = await client.CreateWorkspaceAsync("Q3");

        using var second = await client.CreateWorkspaceAsync("q3");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("A workspace named Q3 exists (names differ only in case): pick another name.", await second.DetailAsync());
        Assert.False(
            Directory.Exists(Path.Combine(app.Root, "workspaces", "q3")) && !Directory.Exists(Path.Combine(app.Root, "workspaces", "Q3"))
        );
    }

    [Fact]
    [Trait("criterion", "AC-windows-compatibility-9")]
    public async Task Renaming_to_another_letter_case_of_an_existing_workspace_is_refused_but_changing_its_own_case_is_allowed()
    {
        using var client = app.CreateClient();
        using var q3 = await client.CreateWorkspaceAsync("Q3");
        using var other = await client.CreateWorkspaceAsync("other");

        using var clash = await client.PatchAsJsonAsync(
            "/api/workspaces/other",
            new { name = "q3" },
            TestContext.Current.CancellationToken
        );
        using var own = await client.PatchAsJsonAsync("/api/workspaces/Q3", new { name = "q3" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
    }
}
