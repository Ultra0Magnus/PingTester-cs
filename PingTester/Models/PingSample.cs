namespace PingTester.Models;

/// <summary>Résultat d'un ping unique vers un hôte.</summary>
public record PingSample(
    string Host,
    DateTime Timestamp,
    long? LatencyMs,   // null = timeout ou erreur
    string Status      // "OK", "TimedOut", "Error", …
);
