using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Suwpptx.Server.Changes;

internal static class ChangeEndpoints
{
    public static void MapChanges(this IEndpointRouteBuilder routes) => routes.MapGet("/changes", Stream);

    private static ServerSentEventsResult<long> Stream(FolderWatch watch, CancellationToken cancellationToken) =>
        TypedResults.ServerSentEvents(Revisions(watch, cancellationToken));

    private static async IAsyncEnumerable<SseItem<long>> Revisions(
        FolderWatch watch,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var seen = watch.Revision;
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return new SseItem<long>(seen, "change");
            seen = await watch.WaitForChangeAsync(seen, cancellationToken);
        }
    }
}
