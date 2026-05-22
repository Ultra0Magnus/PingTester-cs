namespace PingTester.Models;

/// <summary>Phase courante d'un test de débit.</summary>
public enum SpeedPhase
{
    Latency,
    Download,
    Upload
}

/// <summary>Progression poussée pendant le test (latence / download / upload).</summary>
public record SpeedProgress(
    string Phase,        // texte affiché ("Téléchargement…", …)
    double Fraction,     // 0..1 pour la barre de progression
    double? Mbps,        // débit instantané (null pour la phase latence)
    SpeedPhase Which     // quelle mesure est concernée
);

/// <summary>Résultat final d'un test de débit.</summary>
public record SpeedResult(
    double? DownloadMbps,
    double? UploadMbps,
    double? LatencyMs,
    bool Cancelled
);
