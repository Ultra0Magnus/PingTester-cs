using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingTester.Services;

namespace PingTester.ViewModels;

/// <summary>ViewModel de l'onglet Géo-Ping.</summary>
public partial class GeoSearchViewModel : ObservableObject
{
    private readonly IGeoSearchService _geoService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSearching))]
    private bool _isSearching;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotPinging))]
    private bool _isPinging;

    [ObservableProperty] private string _searchText  = "";
    [ObservableProperty] private string _statusText  = "Entrez une ville et cliquez sur Rechercher.";

    public bool IsNotSearching => !IsSearching;
    public bool IsNotPinging   => !IsPinging;

    public ObservableCollection<GeoServerRowViewModel> Results { get; } = [];

    /// <summary>
    /// Déclenché quand l'utilisateur clique « Ajouter au monitoring ».
    /// Transmet la liste des hostnames sélectionnés au MainWindowViewModel.
    /// </summary>
    public event Action<IEnumerable<string>>? AddToMonitorRequested;

    public GeoSearchViewModel() : this(new GeoSearchService()) { }

    public GeoSearchViewModel(IGeoSearchService geoService) => _geoService = geoService;

    // ── Recherche de serveurs ─────────────────────────────────────────────────
    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        IsSearching = true;
        StatusText  = "Recherche en cours…";
        Results.Clear();

        try
        {
            var servers = await _geoService.SearchAsync(SearchText.Trim());
            foreach (var s in servers)
                Results.Add(new GeoServerRowViewModel(s));

            StatusText = Results.Count > 0
                ? $"{Results.Count} serveur(s) trouvé(s) pour « {SearchText.Trim()} »"
                : $"Aucun résultat pour « {SearchText.Trim()} ».";
        }
        catch (Exception ex)
        {
            StatusText = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    // ── Ping unique de chaque serveur sélectionné ─────────────────────────────
    [RelayCommand]
    private async Task PingSelectedAsync()
    {
        var selected = Results.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0) return;

        IsPinging  = true;
        StatusText = "Ping en cours…";

        var tasks = selected.Select(async row =>
        {
            try
            {
                using var ping  = new Ping();
                var reply = await ping.SendPingAsync(row.Server.Host, 3000);

                Dispatcher.UIThread.Post(() =>
                {
                    if (reply.Status == IPStatus.Success)
                        row.SetLatency(reply.RoundtripTime);
                    else
                        row.SetError("Timeout");
                });
            }
            catch
            {
                Dispatcher.UIThread.Post(() => row.SetError("Erreur"));
            }
        });

        await Task.WhenAll(tasks);

        IsPinging  = false;
        StatusText = "Ping terminé.";
    }

    // ── Transfert vers le monitoring principal ────────────────────────────────
    [RelayCommand]
    private void AddToMonitor()
    {
        var hosts = Results
            .Where(r => r.IsSelected)
            .Select(r => r.Server.Host)
            .ToList();

        if (hosts.Count > 0)
            AddToMonitorRequested?.Invoke(hosts);
    }
}
