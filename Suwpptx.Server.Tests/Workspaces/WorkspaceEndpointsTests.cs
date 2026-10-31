using System.Net;

namespace Suwpptx.Server.Tests.Workspaces;

public sealed class WorkspaceEndpointsTests : IDisposable
{
    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    private string WorkspacesFolder => Path.Combine(app.Root, "workspaces");

    [Fact]
    [Trait("criterion", "AC-pptx-studio-1")]
    public async Task A_new_workspace_is_listed_and_starts_with_an_empty_requirements_file()
    {
        using var client = app.CreateClient();

        using var created = await client.CreateWorkspaceAsync("Q3 pitch");
        var list = await client.GetJsonAsync("/api/workspaces");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(["Q3 pitch"], list.EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        var folder = Path.Combine(WorkspacesFolder, "Q3 pitch");
        var context = await File.ReadAllTextAsync(Path.Combine(folder, "context.md"), TestContext.Current.CancellationToken);
        Assert.StartsWith("# Requirements", context, StringComparison.Ordinal);
        foreach (var heading in new[] { "## Audience", "## Goal", "## Must include", "## Style" })
        {
            Assert.Contains(heading, context, StringComparison.Ordinal);
        }
        Assert.True(Directory.Exists(Path.Combine(folder, "resources")));
        Assert.True(Directory.Exists(Path.Combine(folder, "versions")));
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-1")]
    public async Task A_second_workspace_with_the_same_name_is_refused_and_asks_for_another_name()
    {
        using var client = app.CreateClient();
        using var first = await client.CreateWorkspaceAsync("pitch");
        await File.WriteAllTextAsync(
            Path.Combine(WorkspacesFolder, "pitch", "context.md"),
            "my notes",
            TestContext.Current.CancellationToken
        );

        using var second = await client.CreateWorkspaceAsync("pitch");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("pick another name", await second.DetailAsync(), StringComparison.Ordinal);
        Assert.Equal(
            "my notes",
            await File.ReadAllTextAsync(Path.Combine(WorkspacesFolder, "pitch", "context.md"), TestContext.Current.CancellationToken)
        );
    }

    [Theory]
    [Trait("criterion", "AC-pptx-studio-1")]
    [InlineData("../x")]
    [InlineData(".hidden")]
    [InlineData("")]
    [InlineData("a/b")]
    public async Task A_workspace_with_a_bad_name_is_refused_and_nothing_is_created(string name)
    {
        using var client = app.CreateClient();

        using var response = await client.CreateWorkspaceAsync(name);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Empty(Directory.GetFileSystemEntries(WorkspacesFolder));
        Assert.False(Directory.Exists(Path.Combine(app.Root, "x")));
        Assert.Equal(0, (await client.GetJsonAsync("/api/workspaces")).GetArrayLength());
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-12")]
    public async Task Deleting_a_workspace_removes_its_folder_from_disk_and_from_the_list()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("old");
        using var kept = await client.CreateWorkspaceAsync("kept");
        await File.WriteAllTextAsync(
            Path.Combine(WorkspacesFolder, "old", "resources", "notes.txt"),
            "x",
            TestContext.Current.CancellationToken
        );

        using var deleted = await client.DeleteAsync("/api/workspaces/old", TestContext.Current.CancellationToken);
        var list = await client.GetJsonAsync("/api/workspaces");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(Directory.Exists(Path.Combine(WorkspacesFolder, "old")));
        Assert.True(Directory.Exists(Path.Combine(WorkspacesFolder, "kept")));
        Assert.Equal(["kept"], list.EnumerateArray().Select(item => item.GetProperty("name").GetString()));
    }
}
