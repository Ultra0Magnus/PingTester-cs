using System.Net.NetworkInformation;
using PingTester.Models;

namespace PingTester.Services;

/// <summary>
/// Implémentation du service de ping via System.Net.NetworkInformation.
/// Utilise SendPingAsync() natif — pas de subprocess, pas de parsing regex.
/// </summary>
public sealed class PingService : IPingService
{
    public event Action<PingSample>? SampleReceived;

    public Task RunAsync(IEnumerable<string> hosts, int intervalMs, CancellationToken ct)
    {
        var tasks = hosts.Select(h => PingLoopAsync(h, intervalMs, ct));
        return Task.WhenAll(tasks);
    }

    private async Task PingLoopAsync(string host, int intervalMs, CancellationToken ct)
    {
        using var ping = new Ping();

        while (!ct.IsCancellationRequested)
        {
            var started = Environment.TickCount64;
            PingSample sample;

            try
            {
                var reply = await ping.SendPingAsync(host, 3000);

                sample = reply.Status == IPStatus.Success
                    ? new PingSample(host, DateTime.Now, reply.RoundtripTime, "OK")
                    : new PingSample(host, DateTime.Now, null, reply.Status.ToString());
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                sample = new PingSample(host, DateTime.Now, null, msg);
            }

            if (!ct.IsCancellationRequested)
                SampleReceived?.Invoke(sample);

            var elapsed = (int)(Environment.TickCount64 - started);
            var delay = Math.Max(0, intervalMs - elapsed);

            try
            {
                await Task.Delay(delay, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
