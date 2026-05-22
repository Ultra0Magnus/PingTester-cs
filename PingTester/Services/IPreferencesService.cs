using PingTester.Models;

namespace PingTester.Services;

public interface IPreferencesService
{
    /// <summary>Charge les préférences ; renvoie les valeurs par défaut si absentes/corrompues.</summary>
    AppPreferences Load();

    /// <summary>Enregistre les préférences (échec silencieux : ne doit jamais crasher l'app).</summary>
    void Save(AppPreferences prefs);
}
