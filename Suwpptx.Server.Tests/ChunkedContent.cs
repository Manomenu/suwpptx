using System.Net;

namespace Suwpptx.Server.Tests;

// Why: a plain content object announces its length; this one does not, like an upload that streams from a browser
internal sealed class ChunkedContent(byte[] bytes) : HttpContent
{
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        await stream.WriteAsync(bytes, TestContext.Current.CancellationToken);

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
