using System.Text.Json;
using PingTester.Models;

namespace PingTester.Services;

/// <summary>
/// Persistance des préférences via <c>%APPDATA%\PingTester\config.json</c>.
/// Utilise System.Text.Json (BCL) — aucune dépendance ajoutée.
/// </summary>
public sealed class PreferencesService : IPreferencesService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PingTester", "config.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public AppPreferences Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json  = File.ReadAllText(ConfigPath);
                var prefs = JsonSerializer.Deserialize<AppPreferences>(json);
                if (prefs is not null) return prefs;
            }
        }
        catch { /* fichier absent, illisible ou corrompu → on retombe sur les défauts */ }

        return new AppPreferences();
    }

    public void Save(AppPreferences prefs)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (dir is not null) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(prefs, Options);
            File.WriteAllText(ConfigPath, json);
        }
        catch { /* écriture impossible (droits, disque plein…) → on n'empêche pas la fermeture */ }
    }
}
