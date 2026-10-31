using System.Diagnostics;

namespace Suwpptx.Server.Tests;

internal static class Eventually
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(100);

    public static async Task<T> ReturnsAsync<T>(
        Func<Task<T>> read,
        Func<T, bool> accept,
        string expectation,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            var value = await read();
            if (accept(value))
            {
                return value;
            }
            if (elapsed.Elapsed > timeout)
            {
                Assert.Fail($"Not true after {timeout.TotalSeconds:0} s: {expectation}. Last seen: {value}");
            }
            await Task.Delay(Step, cancellationToken);
        }
    }

    public static async Task TrueAsync(Func<bool> condition, string expectation, TimeSpan timeout, CancellationToken cancellationToken) =>
        await ReturnsAsync(() => Task.FromResult(condition()), reached => reached, expectation, timeout, cancellationToken);
}
