using System.Diagnostics;
using System.Globalization;

namespace Suwpptx.Server.Tests;

internal sealed class ChangeStream : IDisposable
{
    private readonly HttpResponseMessage response;

    private readonly StreamReader reader;

    private Task<string?>? pending;

    private bool isChangeEvent;

    private ChangeStream(HttpResponseMessage response, StreamReader reader)
    {
        this.response = response;
        this.reader = reader;
    }

    public static async Task<ChangeStream> OpenAsync(HttpClient client)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/changes");
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        return new ChangeStream(response, new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken)));
    }

    // Why: a read is never cancelled half-way (that would corrupt the reader), so a late line is kept for the next call
    public async Task<long?> NextRevisionAsync(TimeSpan within)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            pending ??= reader.ReadLineAsync(cancellationToken).AsTask();
            var left = within - elapsed.Elapsed;
            if (left <= TimeSpan.Zero || await Task.WhenAny(pending, Task.Delay(left, cancellationToken)) != pending)
            {
                return null;
            }
            var line = await pending;
            pending = null;
            Assert.NotNull(line);
            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                isChangeEvent = line["event:".Length..].Trim() == "change";
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal) && isChangeEvent)
            {
                return long.Parse(line["data:".Length..].Trim(), CultureInfo.InvariantCulture);
            }
        }
    }

    // Why: the watcher may report the folder it just created at start-up, so the test acts only after the stream is quiet
    public async Task<long> SettledRevisionAsync()
    {
        var first = await NextRevisionAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(first);
        var revision = first.Value;
        while (await NextRevisionAsync(TimeSpan.FromSeconds(1.5)) is { } later)
        {
            revision = later;
        }
        return revision;
    }

    public void Dispose()
    {
        reader.Dispose();
        response.Dispose();
    }
}
