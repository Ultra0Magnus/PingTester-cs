using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PingTester.Statistics;

namespace PingTester.ViewModels;

/// <summary>État observé pour un hôte : compteurs, données graphe et statistiques.</summary>
public partial class HostStateViewModel : ObservableObject
{
    public string Host { get; }

    // ── Compteurs (ObservableProperty pour les bindings directs) ─────────────
    [ObservableProperty] private int    _sent;
    [ObservableProperty] private int    _lost;
    [ObservableProperty] private double _lastLatency = -1;

    // ── Textes barre de statut ────────────────────────────────────────────────
    public string LossText    => Sent == 0 ? "0 %" : $"{Lost * 100.0 / Sent:F1} %";
    public string LatencyText => LastLatency < 0 ? "—" : $"{LastLatency:F0} ms";

    // ── Textes onglet Stats ───────────────────────────────────────────────────
    public string MinText    => _validLatencies.Count == 0 ? "—" : $"{_min:F0} ms";
    public string MaxText    => _validLatencies.Count == 0 ? "—" : $"{_max:F0} ms";
    public string AvgText    => _validLatencies.Count == 0 ? "—" : $"{_sum / _validLatencies.Count:F1} ms";
    public string MedianText => _validLatencies.Count == 0 ? "—" : $"{Quality.Median(_validLatencies):F1} ms";
    public string JitterText => _validLatencies.Count < 2  ? "—" : $"{Quality.ComputeJitter(_validLatencies):F1} ms";

    public string QualityScore => _validLatencies.Count == 0
        ? "—"
        : Quality.ComputeScore(
            Sent == 0 ? 0 : Lost * 100.0 / Sent,
            _sum / _validLatencies.Count,
            Quality.ComputeJitter(_validLatencies));

    public IBrush QualityBrush =>
        new SolidColorBrush(Color.Parse(Quality.ScoreColor(QualityScore)));

    // ── Données internes ──────────────────────────────────────────────────────
    private readonly List<double> _validLatencies = [];
    private double _min = double.MaxValue, _max = 0, _sum = 0;

    // Données pour le graphe ScottPlot (X = numéro du ping, Y = ms ou NaN)
    public List<double> XData { get; } = [];
    public List<double> YData { get; } = [];

    public HostStateViewModel(string host) => Host = host;

    // ── Ajout d'un résultat ───────────────────────────────────────────────────
    public void AddSample(long? latencyMs)
    {
        Sent++;
        XData.Add(Sent);

        if (latencyMs is null)
        {
            Lost++;
            YData.Add(double.NaN);
        }
        else
        {
            double v = latencyMs.Value;
            YData.Add(v);
            LastLatency = v;

            _validLatencies.Add(v);
            _sum += v;
            if (v < _min) _min = v;
            if (v > _max) _max = v;
        }

        // Notifie toutes les propriétés calculées à la fois
        OnPropertyChanged(nameof(LossText));
        OnPropertyChanged(nameof(LatencyText));
        OnPropertyChanged(nameof(MinText));
        OnPropertyChanged(nameof(MaxText));
        OnPropertyChanged(nameof(AvgText));
        OnPropertyChanged(nameof(MedianText));
        OnPropertyChanged(nameof(JitterText));
        OnPropertyChanged(nameof(QualityScore));
        OnPropertyChanged(nameof(QualityBrush));
    }
}
