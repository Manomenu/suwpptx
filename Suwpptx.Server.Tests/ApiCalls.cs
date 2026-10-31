using System.Net.Http.Json;
using System.Text.Json;

namespace Suwpptx.Server.Tests;

internal static class ApiCalls
{
    public static async Task<HttpResponseMessage> CreateWorkspaceAsync(this HttpClient client, string name) =>
        await client.PostAsJsonAsync("/api/workspaces", new { name }, TestContext.Current.CancellationToken);

    public static async Task<HttpResponseMessage> UploadAsync(this HttpClient client, string url, byte[] bytes) =>
        await PutBytesAsync(client, url, bytes);

    private static async Task<HttpResponseMessage> PutBytesAsync(HttpClient client, string url, byte[] bytes)
    {
        using var content = new ByteArrayContent(bytes);
        return await client.PutAsync(url, content, TestContext.Current.CancellationToken);
    }

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(text).RootElement;
    }

    public static async Task<JsonElement> GetJsonAsync(this HttpClient client, string url)
    {
        using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        return await response.JsonAsync();
    }

    public static async Task<string> DetailAsync(this HttpResponseMessage response) =>
        (await response.JsonAsync()).GetProperty("detail").GetString() ?? "";
}
