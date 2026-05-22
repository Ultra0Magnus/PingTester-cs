using System.Text.Json;
using System.Text.Json.Serialization;
using PingTester.Models;

namespace PingTester.Services;

/// <summary>
/// Interroge l'API publique Speedtest.net pour trouver des serveurs de test
/// à proximité d'une ville donnée. Aucune clé API requise.
/// </summary>
public sealed class GeoSearchService : IGeoSearchService
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) PingTester/0.3");
        client.DefaultRequestHeaders.Add("Referer", "https://www.speedtest.net/");
        return client;
    }

    public async Task<List<GeoServer>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        try
        {
            var url = $"https://www.speedtest.net/api/js/servers" +
                      $"?engine=js&search={Uri.EscapeDataString(query)}&limit=5&https_functional=true";

            var json = await Http.GetStringAsync(url, ct);

            var dtos = JsonSerializer.Deserialize<List<SpeedtestServerDto>>(json,
                           new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dtos is null) return [];

            var result = new List<GeoServer>();
            foreach (var dto in dtos)
            {
                // Le champ "host" est au format "hostname:port" → on garde uniquement le hostname
                var host = dto.Host.Split(':')[0];
                if (string.IsNullOrWhiteSpace(host)) continue;

                double.TryParse(dto.Lat, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat);
                double.TryParse(dto.Lon, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lon);

                result.Add(new GeoServer(
                    Name:    dto.Name,
                    Host:    host,
                    Country: dto.Country,
                    Sponsor: dto.Sponsor,
                    Lat:     lat,
                    Lon:     lon
                ));
            }
            return result;
        }
        catch
        {
            // Réseau indisponible, API modifiée, etc. → on ne crashe jamais l'app
            return [];
        }
    }

    // ── DTO interne pour la désérialisation JSON ──────────────────────────────
    private sealed class SpeedtestServerDto
    {
        [JsonPropertyName("name")]    public string Name    { get; set; } = "";
        [JsonPropertyName("host")]    public string Host    { get; set; } = "";
        [JsonPropertyName("country")] public string Country { get; set; } = "";
        [JsonPropertyName("sponsor")] public string Sponsor { get; set; } = "";
        [JsonPropertyName("lat")]     public string Lat     { get; set; } = "0";
        [JsonPropertyName("lon")]     public string Lon     { get; set; } = "0";
    }
}
