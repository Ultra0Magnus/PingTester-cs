using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using System.Runtime.InteropServices;
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

    // ── Sauvegarde des préférences à la fermeture de la fenêtre ───────────────
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.SavePreferences();
        base.OnClosing(e);
    }

    // ── Initialisation après chargement ──────────────────────────────────────
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _vm = DataContext as MainWindowViewModel;
        if (_vm is null) return;

        _vm.SampleArrived += _ => RefreshPlot();
        _vm.PingStarted    += OnPingStarted;
        _vm.AlertTriggered += OnAlertTriggered;

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

        // Limites initiales : pas de valeurs négatives sur les axes
        plt.Axes.SetLimits(left: 0, right: 10, bottom: 0, top: 100);

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
            // ── Seuil : ligne + marqueurs sur les points qui le dépassent ────
            double threshold = (double)_vm.ThresholdMs;
            if (threshold > 0)
            {
                var hLine = plt.Add.HorizontalLine(threshold);
                hLine.Color     = Color.FromHex("#f38ba8");
                hLine.LineWidth = 1.5f;

                foreach (var host in _vm.Hosts)
                {
                    var axs = new List<double>();
                    var ays = new List<double>();
                    for (int j = 0; j < host.YData.Count; j++)
                        if (!double.IsNaN(host.YData[j]) && host.YData[j] > threshold)
                        { axs.Add(host.XData[j]); ays.Add(host.YData[j]); }

                    if (axs.Count > 0)
                    {
                        var mark = plt.Add.Scatter(axs.ToArray(), ays.ToArray());
                        mark.Color      = Color.FromHex("#f38ba8");
                        mark.LineWidth  = 0;
                        mark.MarkerSize = 9;
                    }
                }
            }

            plt.Legend.IsVisible = true;
            plt.Legend.Alignment = Alignment.UpperRight;

            plt.Axes.AutoScale();
            var lim    = plt.Axes.GetLimits();
            double top = Math.Max(lim.Top * 1.10, 10);
            plt.Axes.SetLimits(lim.Left, lim.Right, 0, top);

            // ── Marqueurs ambre pour les timeouts (NaN → en haut du graphe) ─
            double timeoutY = top * 0.88;
            foreach (var host in _vm.Hosts)
            {
                var txs = new List<double>();
                for (int j = 0; j < host.YData.Count; j++)
                    if (double.IsNaN(host.YData[j])) txs.Add(host.XData[j]);

                if (txs.Count > 0)
                {
                    var tys  = Enumerable.Repeat(timeoutY, txs.Count).ToArray();
                    var mark = plt.Add.Scatter(txs.ToArray(), tys);
                    mark.Color      = Color.FromHex("#f9e2af");   // ambre
                    mark.LineWidth  = 0;
                    mark.MarkerSize = 9;
                }
            }
        }

        AvaPlot1.Refresh();
    }

    // ── Alerte : flash de la barre des tâches ─────────────────────────────────
    private void OnAlertTriggered()
    {
        var handle = TryGetPlatformHandle();
        if (handle is not null && handle.Handle != IntPtr.Zero)
            NativeMethods.Flash(handle.Handle);
    }

    // ── P/Invoke : FlashWindowEx (user32.dll) ────────────────────────────────
    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint   cbSize;
            public IntPtr hwnd;
            public uint   dwFlags;
            public uint   uCount;
            public uint   dwTimeout;
        }

        private const uint FLASHW_ALL       = 3;
        private const uint FLASHW_TIMERNOFG = 12;

        public static void Flash(IntPtr hwnd)
        {
            var info = new FLASHWINFO
            {
                cbSize    = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd      = hwnd,
                dwFlags   = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount    = 3,
                dwTimeout = 0,
            };
            FlashWindowEx(ref info);
        }
    }
}
