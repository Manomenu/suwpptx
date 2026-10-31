namespace Suwpptx.Server.Tests.Changes;

public sealed class ChangeEndpointsTests : IDisposable
{
    private static readonly TimeSpan Arrival = TimeSpan.FromSeconds(5);

    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    [Trait("criterion", "AC-pptx-studio-13")]
    public async Task A_file_created_straight_on_disk_raises_the_revision_within_5_seconds()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        using var changes = await ChangeStream.OpenAsync(client);
        var before = await changes.SettledRevisionAsync();

        await File.WriteAllTextAsync(
            Path.Combine(app.Root, "workspaces", "ws", "resources", "dropped-in.txt"),
            "hello",
            TestContext.Current.CancellationToken
        );

        var after = await changes.NextRevisionAsync(Arrival);
        Assert.NotNull(after);
        Assert.True(after > before, $"revision {after} should be higher than {before}");
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-13")]
    public async Task A_file_deleted_straight_on_disk_raises_the_revision_within_5_seconds()
    {
        using var client = app.CreateClient();
        using var created = await client.CreateWorkspaceAsync("ws");
        var file = Path.Combine(app.Root, "workspaces", "ws", "resources", "going.txt");
        await File.WriteAllTextAsync(file, "bye", TestContext.Current.CancellationToken);
        using var changes = await ChangeStream.OpenAsync(client);
        var before = await changes.SettledRevisionAsync();

        File.Delete(file);

        var after = await changes.NextRevisionAsync(Arrival);
        Assert.NotNull(after);
        Assert.True(after > before, $"revision {after} should be higher than {before}");
    }

    [Fact]
    [Trait("criterion", "AC-pptx-studio-13")]
    public async Task Without_any_change_the_revision_stays_where_it_was()
    {
        using var client = app.CreateClient();
        using var changes = await ChangeStream.OpenAsync(client);
        await changes.SettledRevisionAsync();

        var quiet = await changes.NextRevisionAsync(TimeSpan.FromSeconds(2));

        Assert.Null(quiet);
    }
}
