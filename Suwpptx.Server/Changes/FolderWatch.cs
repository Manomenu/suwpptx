using Suwpptx.Server.Folder;

namespace Suwpptx.Server.Changes;

internal sealed class FolderWatch(RootFolder root, TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(500);

    private static readonly EnumerationOptions Everything = new()
    {
        AttributesToSkip = 0,
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
    };

    private long revision;

    private TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public long Revision => Volatile.Read(ref revision);

    // Why: the wake-up source is read before the revision is compared, and a change raises the revision before it wakes anyone, so no change is missed
    public async Task<long> WaitForChangeAsync(long seen, CancellationToken cancellationToken)
    {
        while (true)
        {
            var wake = Volatile.Read(ref changed);
            if (Revision != seen)
            {
                return Revision;
            }
            await wake.Task.WaitAsync(cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var last = Fingerprint();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            var now = Fingerprint();
            if (now != last)
            {
                last = now;
                Interlocked.Increment(ref revision);
                Interlocked
                    .Exchange(ref changed, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
                    .TrySetResult();
            }
        }
    }

    private int Fingerprint()
    {
        var hash = new HashCode();
        if (!Directory.Exists(root.Path))
        {
            return hash.ToHashCode();
        }
        foreach (var entry in new DirectoryInfo(root.Path).EnumerateFileSystemInfos("*", Everything))
        {
            hash.Add(entry.FullName, StringComparer.Ordinal);
            hash.Add(entry is FileInfo file ? file.Length : -1);
            hash.Add(entry.LastWriteTimeUtc.Ticks);
        }
        return hash.ToHashCode();
    }
}
