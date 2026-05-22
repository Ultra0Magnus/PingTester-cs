using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingTester.Models;
using PingTester.Services;

namespace PingTester.ViewModels;

/// <summary>Onglet « Débit » : pilote le test Cloudflare (latence / download / upload).</summary>
public partial class SpeedTestViewModel : ObservableObject
{
    private readonly ISpeedTestService _service;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonLabel))]
    private bool _isRunning;

    [ObservableProperty] private double _progress;                 // 0..1
    [ObservableProperty] private string _phase        = "Prêt — clique pour lancer un test.";
    [ObservableProperty] private string _downloadText = "—";
    [ObservableProperty] private string _uploadText   = "—";
    [ObservableProperty] private string _latencyText  = "—";

    public string ButtonLabel => IsRunning ? "■ Annuler" : "▶ Tester le débit";

    public SpeedTestViewModel() : this(new SpeedTestService()) { }

    public SpeedTestViewModel(ISpeedTestService service) => _service = service;

    [RelayCommand]
    private void ToggleSpeedTest()
    {
        if (IsRunning)
        {
            _cts?.Cancel();
            Phase = "Annulation…";
            return;
        }

        // Reset de l'affichage
        IsRunning    = true;
        Progress     = 0;
        DownloadText = "…";
        UploadText   = "—";
        LatencyText  = "—";
        Phase        = "Mesure de la latence…";

        // Progress<T> créé ici (thread UI) → OnProgress sera appelé sur le thread UI
        var progress = new Progress<SpeedProgress>(OnProgress);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await _service.RunAsync(progress, ct);
                Dispatcher.UIThread.Post(() => OnDone(result));
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => OnError(ex.Message));
            }
        });
    }

    // ── Progression (déjà marshalée sur le thread UI par Progress<T>) ─────────
    private void OnProgress(SpeedProgress p)
    {
        Phase    = p.Phase;
        Progress = p.Fraction;
        if (p.Mbps is not null)
        {
            if (p.Which == SpeedPhase.Download) DownloadText = $"{p.Mbps:F1}";
            else if (p.Which == SpeedPhase.Upload) UploadText = $"{p.Mbps:F1}";
        }
    }

    private void OnDone(SpeedResult r)
    {
        IsRunning    = false;
        Progress     = 0;
        DownloadText = r.DownloadMbps is null ? "—" : $"{r.DownloadMbps:F1}";
        UploadText   = r.UploadMbps   is null ? "—" : $"{r.UploadMbps:F1}";
        LatencyText  = r.LatencyMs    is null ? "—" : $"{r.LatencyMs:F0}";
        Phase        = r.Cancelled ? "Test annulé." : "Terminé.";
    }

    private void OnError(string message)
    {
        IsRunning = false;
        Progress  = 0;
        Phase     = $"Échec du test : {message}";
    }
}
