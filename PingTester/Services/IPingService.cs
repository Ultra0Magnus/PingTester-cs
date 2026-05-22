using PingTester.Models;

namespace PingTester.Services;

public interface IPingService
{
    event Action<PingSample>? SampleReceived;

    /// <summary>
    /// Démarre une boucle de ping concurrente pour chaque hôte.
    /// Se termine quand <paramref name="ct"/> est annulé.
    /// </summary>
    Task RunAsync(IEnumerable<string> hosts, int intervalMs, CancellationToken ct);
}
