using PingTester.Models;

namespace PingTester.Services;

public interface ISpeedTestService
{
    /// <summary>
    /// Mesure latence → download → upload (séquentiel, via Cloudflare).
    /// Annulable : renvoie un <see cref="SpeedResult"/> avec les valeurs déjà
    /// mesurées et Cancelled = true.
    /// </summary>
    Task<SpeedResult> RunAsync(IProgress<SpeedProgress> progress, CancellationToken ct);
}
