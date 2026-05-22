using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PingTester.ViewModels;
using PingTester.Views;
using Velopack;
using Velopack.Sources;

namespace PingTester;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            // Vérification silencieuse des MAJ en arrière-plan (fire-and-forget)
            _ = Task.Run(CheckForUpdatesAsync);
        }

        base.OnFrameworkInitializationCompleted();
    }

    // ── Mise à jour silencieuse depuis GitHub Releases ────────────────────────
    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            var source = new GithubSource(
                "https://github.com/Ultra0Magnus/PingTester-cs",
                accessToken: null,
                prerelease: false);

            var mgr = new UpdateManager(source);
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion != null)
                await mgr.DownloadUpdatesAsync(newVersion);
            // La MAJ est appliquée automatiquement au prochain lancement par Velopack
        }
        catch
        {
            // Silencieux : pas d'internet, app non installée via Velopack, etc.
        }
    }
}
