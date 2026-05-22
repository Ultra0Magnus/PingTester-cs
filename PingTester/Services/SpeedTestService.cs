using System.Diagnostics;
using System.Net.Http.Headers;
using PingTester.Models;

namespace PingTester.Services;

/// <summary>
/// Test de débit via les endpoints Cloudflare (port de la version Python).
/// Utilise HttpClient + async au lieu d'urllib + threading.
/// </summary>
public sealed class SpeedTestService : ISpeedTestService
{
    // Endpoints + plafonds — identiques à la version Python
    private const string DownUrl   = "https://speed.cloudflare.com/__down?bytes={0}";
    private const string UpUrl     = "https://speed.cloudflare.com/__up";
    private const long   DlBytes   = 80_000_000;   // plafond download
    private const double DlSeconds = 10.0;
    private const int    UlChunk   = 5_000_000;    // taille d'un POST upload
    private const double UlSeconds = 10.0;

    private readonly HttpClient _http;

    public SpeedTestService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Add("User-Agent", "PingTester");
    }

    public async Task<SpeedResult> RunAsync(IProgress<SpeedProgress> progress, CancellationToken ct)
    {
        double? lat = null, dl = null, ul = null;
        try
        {
            progress.Report(new SpeedProgress("Mesure de la latence…", 0, null, SpeedPhase.Latency));
            lat = await MeasureLatencyAsync(ct);
            if (ct.IsCancellationRequested) return new SpeedResult(dl, ul, lat, true);

            progress.Report(new SpeedProgress("Téléchargement…", 0, null, SpeedPhase.Download));
            dl = await MeasureDownloadAsync(progress, ct);
            if (ct.IsCancellationRequested) return new SpeedResult(dl, ul, lat, true);

            progress.Report(new SpeedProgress("Envoi…", 0, null, SpeedPhase.Upload));
            ul = await MeasureUploadAsync(progress, ct);

            return new SpeedResult(dl, ul, lat, ct.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
            return new SpeedResult(dl, ul, lat, true);
        }
    }

    // ── Latence : min de 5 requêtes __down?bytes=0 ────────────────────────────
    private async Task<double?> MeasureLatencyAsync(CancellationToken ct)
    {
        var times = new List<double>();
        for (int i = 0; i < 5; i++)
        {
            if (ct.IsCancellationRequested) break;
            var sw = Stopwatch.StartNew();
            try
            {
                using var resp = await _http.GetAsync(string.Format(DownUrl, 0), ct);
                resp.EnsureSuccessStatusCode();
                await resp.Content.ReadAsByteArrayAsync(ct);
                times.Add(sw.Elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) { break; }
            catch { /* on ignore les échecs individuels */ }
        }
        return times.Count > 0 ? times.Min() : null;
    }

    // ── Download : flux __down?bytes=80M, lu par buffers de 128 Ko ────────────
    private async Task<double> MeasureDownloadAsync(IProgress<SpeedProgress> progress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        long read = 0;
        double lastReport = 0;
        try
        {
            using var resp = await _http.GetAsync(
                string.Format(DownUrl, DlBytes),
                HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var buffer = new byte[131072];

            while (!ct.IsCancellationRequested)
            {
                int n = await stream.ReadAsync(buffer, ct);
                if (n == 0) break;
                read += n;

                double elapsed = sw.Elapsed.TotalSeconds;
                if (elapsed - lastReport >= 0.12)
                {
                    lastReport = elapsed;
                    double mbps = elapsed > 0 ? read * 8.0 / elapsed / 1e6 : 0;
                    progress.Report(new SpeedProgress("Téléchargement…",
                        Math.Min(1.0, elapsed / DlSeconds), mbps, SpeedPhase.Download));
                }
                if (elapsed >= DlSeconds) break;
            }
        }
        catch (OperationCanceledException) { /* renvoie le partiel */ }

        double total = sw.Elapsed.TotalSeconds;
        return total > 0 ? read * 8.0 / total / 1e6 : 0.0;
    }

    // ── Upload : POST répétés d'un payload de 5 Mo ────────────────────────────
    private async Task<double> MeasureUploadAsync(IProgress<SpeedProgress> progress, CancellationToken ct)
    {
        var payload = new byte[UlChunk];   // 5 Mo de zéros
        var sw = Stopwatch.StartNew();
        long sent = 0;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                using var content = new ByteArrayContent(payload);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                using var resp = await _http.PostAsync(UpUrl, content, ct);
                resp.EnsureSuccessStatusCode();
                await resp.Content.ReadAsByteArrayAsync(ct);

                sent += UlChunk;
                double elapsed = sw.Elapsed.TotalSeconds;
                double mbps = elapsed > 0 ? sent * 8.0 / elapsed / 1e6 : 0;
                progress.Report(new SpeedProgress("Envoi…",
                    Math.Min(1.0, elapsed / UlSeconds), mbps, SpeedPhase.Upload));

                if (elapsed >= UlSeconds) break;
            }
        }
        catch (OperationCanceledException) { /* renvoie le partiel */ }

        double total = sw.Elapsed.TotalSeconds;
        return total > 0 ? sent * 8.0 / total / 1e6 : 0.0;
    }
}
