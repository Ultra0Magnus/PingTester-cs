using PingTester.Models;

namespace PingTester.Services;

/// <summary>Recherche des serveurs de test par nom de ville via Speedtest.net.</summary>
public interface IGeoSearchService
{
    /// <summary>
    /// Renvoie jusqu'à 5 serveurs correspondant à la requête.
    /// Retourne une liste vide en cas d'erreur réseau (ne lève jamais).
    /// </summary>
    Task<List<GeoServer>> SearchAsync(string query, CancellationToken ct = default);
}
