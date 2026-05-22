using Avalonia.Controls;
using Avalonia.Interactivity;
using PingTester.Models;
using PingTester.ViewModels;
using ScottPlot;

namespace PingTester.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    // Palette catppuccin-mocha pour le thème sombre
    private static readonly Color[] Palette =
    [
        Color.FromHex("#89b4fa"),   // blue
        Color.FromHex("#a6e3a1"),   // green
        Color.FromHex("#fab387"),   // peach
        Color.FromHex("#f38ba8"),   // red
        Color.FromHex("#cba6f7"),   // mauve
        Color.FromHex("#f9e2af"),   // yellow
        Color.FromHex("#94e2d5"),   // teal
        Color.FromHex("#74c7ec"),   // sapphire
    ];

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    // ── Initialisation après chargement ──────────────────────────────────────
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _vm = DataContext as MainWindowViewModel;
        if (_vm is null) return;

        _vm.SampleArrived += _ => RefreshPlot();
        _vm.PingStarted   += OnPingStarted;

        ApplyDarkTheme();
    }

    // ── Thème sombre ScottPlot ────────────────────────────────────────────────
    private void ApplyDarkTheme()
    {
        var plt = AvaPlot1.Plot;

        plt.FigureBackground.Color = Color.FromHex("#1e1e2e");
        plt.DataBackground.Color   = Color.FromHex("#181825");

        plt.Grid.MajorLineColor = Color.FromHex("#313244");
        plt.Grid.MinorLineColor = Color.FromHex("#22223b");
        plt.Grid.MinorLineWidth = 0.5f;

        // Couleur du texte des axes
        plt.Axes.Color(Color.FromHex("#cdd6f4"));

        plt.YLabel("Latence (ms)");
        plt.XLabel("N° ping");

        AvaPlot1.Refresh();
    }

    // ── Nouveau démarrage → vider le graphe ───────────────────────────────────
    private void OnPingStarted()
    {
        AvaPlot1.Plot.Clear();
        AvaPlot1.Refresh();
    }

    // ── Mise à jour du graphe à chaque nouveau sample ─────────────────────────
    private void RefreshPlot()
    {
        if (_vm is null) return;

        var plt = AvaPlot1.Plot;
        plt.Clear();    // supprime les plottables, pas les labels ni le style

        int i = 0;
        foreach (var host in _vm.Hosts)
        {
            if (host.XData.Count == 0) continue;

            var xs = host.XData.ToArray();
            var ys = host.YData.ToArray();

            var sp = plt.Add.Scatter(xs, ys);
            sp.LegendText = host.Host;
            sp.Color      = Palette[i % Palette.Length];
            sp.MarkerSize = 4;
            sp.LineWidth  = 1.5f;
            i++;
        }

        if (i > 0)
        {
            plt.Legend.IsVisible = true;
            plt.Legend.Alignment = Alignment.UpperRight;
        }

        AvaPlot1.Refresh();
    }
}
