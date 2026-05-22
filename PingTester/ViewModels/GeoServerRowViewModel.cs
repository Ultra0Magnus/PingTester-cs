using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PingTester.Models;

namespace PingTester.ViewModels;

/// <summary>Ligne de résultat dans l'onglet Géo-Ping.</summary>
public partial class GeoServerRowViewModel : ObservableObject
{
    public GeoServer Server { get; }

    [ObservableProperty] private bool    _isSelected    = true;
    [ObservableProperty] private string  _latencyText   = "—";
    [ObservableProperty] private IBrush  _latencyBrush  = Brushes.Transparent;
    [ObservableProperty] private IBrush  _latencyForeground = new SolidColorBrush(Color.Parse("#6c7086"));

    public GeoServerRowViewModel(GeoServer server) => Server = server;

    /// <summary>Applique la latence mesurée et choisit la couleur associée.</summary>
    public void SetLatency(long ms)
    {
        LatencyText = $"{ms} ms";

        // Palette Catppuccin-Mocha : vert / ambre / pêche / rouge
        var (bg, fg) = ms switch
        {
            < 50  => ("#a6e3a1", "#1e1e2e"),
            < 150 => ("#f9e2af", "#1e1e2e"),
            < 300 => ("#fab387", "#1e1e2e"),
            _     => ("#f38ba8", "#1e1e2e"),
        };
        LatencyBrush      = new SolidColorBrush(Color.Parse(bg));
        LatencyForeground = new SolidColorBrush(Color.Parse(fg));
    }

    /// <summary>Marque la ligne comme timeout ou erreur.</summary>
    public void SetError(string label = "Timeout")
    {
        LatencyText       = label;
        LatencyBrush      = new SolidColorBrush(Color.Parse("#f38ba8"));
        LatencyForeground = new SolidColorBrush(Color.Parse("#1e1e2e"));
    }
}
