using CommunityToolkit.Mvvm.ComponentModel;

namespace PingTester.ViewModels;

/// <summary>État observé pour un hôte (compteurs + données du graphe).</summary>
public partial class HostStateViewModel : ObservableObject
{
    public string Host { get; }

    [ObservableProperty] private int _sent;
    [ObservableProperty] private int _lost;
    [ObservableProperty] private double _lastLatency = -1;

    /// <summary>Chaîne affichant le taux de perte, ex. "3.5%".</summary>
    public string LossText  => Sent == 0 ? "0 %" : $"{Lost * 100.0 / Sent:F1} %";

    /// <summary>Chaîne affichant la dernière latence, ex. "12 ms".</summary>
    public string LatencyText => LastLatency < 0 ? "—" : $"{LastLatency:F0} ms";

    // Données pour ScottPlot : X = numéro du ping, Y = latence en ms (NaN = timeout)
    public List<double> XData { get; } = [];
    public List<double> YData { get; } = [];

    public HostStateViewModel(string host) => Host = host;

    /// <summary>Enregistre un nouveau résultat et met à jour les compteurs.</summary>
    public void AddSample(long? latencyMs)
    {
        Sent++;
        if (latencyMs is null)
        {
            Lost++;
            XData.Add(Sent);
            YData.Add(double.NaN);
        }
        else
        {
            XData.Add(Sent);
            YData.Add((double)latencyMs);
            LastLatency = latencyMs.Value;
        }
        OnPropertyChanged(nameof(LossText));
        OnPropertyChanged(nameof(LatencyText));
    }
}
