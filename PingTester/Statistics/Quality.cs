namespace PingTester.Statistics;

/// <summary>Calculs statistiques et score de qualité (port de la version Python).</summary>
public static class Quality
{
    // ── Statistiques ─────────────────────────────────────────────────────────

    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToArray();
        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    /// <summary>Gigue = moyenne des écarts entre pings consécutifs.</summary>
    public static double ComputeJitter(IReadOnlyList<double> values)
    {
        if (values.Count < 2) return 0;
        double sum = 0;
        for (int i = 1; i < values.Count; i++)
            sum += Math.Abs(values[i] - values[i - 1]);
        return sum / (values.Count - 1);
    }

    // ── Score de qualité A–F ──────────────────────────────────────────────────

    public static string ComputeScore(double lossPct, double avgMs, double jitterMs)
    {
        if (lossPct == 0   && avgMs < 20  && jitterMs < 3)   return "A+";
        if (lossPct < 1    && avgMs < 50  && jitterMs < 5)   return "A";
        if (lossPct < 3    && avgMs < 100 && jitterMs < 10)  return "B";
        if (lossPct < 5    && avgMs < 150 && jitterMs < 20)  return "C";
        if (lossPct < 10   && avgMs < 300 && jitterMs < 50)  return "D";
        return "F";
    }

    public static string ScoreColor(string score) => score switch
    {
        "A+" => "#a6e3a1",  // vert vif
        "A"  => "#94e2d5",  // teal
        "B"  => "#89b4fa",  // bleu
        "C"  => "#f9e2af",  // jaune
        "D"  => "#fab387",  // orange
        "F"  => "#f38ba8",  // rouge
        _    => "#585b70",  // gris (pas encore mesuré)
    };
}
