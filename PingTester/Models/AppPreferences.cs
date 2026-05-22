namespace PingTester.Models;

/// <summary>
/// Préférences persistées entre deux sessions (sérialisées en JSON dans
/// <c>%APPDATA%\PingTester\config.json</c>). POCO mutable pour System.Text.Json.
/// </summary>
public sealed class AppPreferences
{
    public string HostsText   { get; set; } = "8.8.8.8, 1.1.1.1";
    public double IntervalMs  { get; set; } = 1000;
    public int    SelectedTab { get; set; } = 0;
    public double ThresholdMs { get; set; } = 0;
}
