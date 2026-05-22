using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingTester.Models;
using PingTester.Services;

namespace PingTester.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // ── Services ──────────────────────────────────────────────────────────────
    private readonly IPingService _pingService;
    private CancellationTokenSource? _cts;

    // ── Propriétés observables ────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartStopLabel), nameof(IsNotRunning))]
    private bool _isRunning;

    [ObservableProperty] private string _hostsText  = "8.8.8.8, 1.1.1.1";
    [ObservableProperty] private decimal _intervalMs = 1000;
    [ObservableProperty] private string _statusText  = "Prêt — saisir des hôtes et cliquer ▶ Démarrer";

    public bool   IsNotRunning   => !IsRunning;
    public string StartStopLabel => IsRunning ? "⏹ Arrêter" : "▶ Démarrer";

    // ── Collections ───────────────────────────────────────────────────────────
    public ObservableCollection<HostStateViewModel> Hosts { get; } = [];

    // ── Événements pour le code-behind (graphe ScottPlot) ────────────────────
    public event Action<PingSample>? SampleArrived;
    public event Action? PingStarted;

    // ── Constructeurs ─────────────────────────────────────────────────────────
    public MainWindowViewModel() : this(new PingService()) { }

    public MainWindowViewModel(IPingService pingService)
    {
        _pingService = pingService;
        _pingService.SampleReceived += OnSampleReceived;
    }

    // ── Réception d'un résultat (appelé depuis n'importe quel thread) ─────────
    private void OnSampleReceived(PingSample sample)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = Hosts.FirstOrDefault(h => h.Host == sample.Host);
            if (vm is null) return;

            vm.AddSample(sample.LatencyMs);

            var sent = Hosts.Sum(h => h.Sent);
            var lost = Hosts.Sum(h => h.Lost);
            StatusText = sent == 0
                ? "Ping en cours…"
                : $"Envoyés : {sent}   Perdus : {lost}   ({lost * 100.0 / sent:F1} %)";

            SampleArrived?.Invoke(sample);
        });
    }

    // ── Commande Démarrer / Arrêter ───────────────────────────────────────────
    [RelayCommand]
    private void StartStop()
    {
        if (IsRunning)
        {
            _cts?.Cancel();
            return;   // IsRunning sera mis à false dans le finally du Task.Run
        }

        var hosts = HostsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

        if (hosts.Count == 0)
        {
            StatusText = "⚠ Aucun hôte saisi";
            return;
        }

        // Réinitialise l'état
        Hosts.Clear();
        foreach (var h in hosts)
            Hosts.Add(new HostStateViewModel(h));

        IsRunning = true;
        StatusText = "Ping en cours…";
        PingStarted?.Invoke();   // le code-behind efface le graphe

        _cts = new CancellationTokenSource();
        var ct       = _cts.Token;
        var interval = (int)IntervalMs;

        // Lance la boucle de ping en arrière-plan (fire-and-forget géré)
        _ = Task.Run(async () =>
        {
            try
            {
                await _pingService.RunAsync(hosts, interval, ct);
            }
            catch (OperationCanceledException) { /* annulation normale */ }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                    StatusText = $"Erreur : {ex.Message}");
            }
            finally
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsRunning = false;
                    var sent = Hosts.Sum(h => h.Sent);
                    var lost = Hosts.Sum(h => h.Lost);
                    StatusText = $"Arrêté — Envoyés : {sent}   Perdus : {lost}";
                });
            }
        });
    }
}
