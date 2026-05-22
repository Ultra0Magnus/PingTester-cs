namespace PingTester.Models;

/// <summary>Serveur de test renvoyé par la recherche géographique.</summary>
public sealed record GeoServer(
    string Name,      // Nom du serveur / ville (ex. "Chicago, Illinois")
    string Host,      // Hostname pingable, sans port (ex. "speedtest.att.net")
    string Country,   // Pays (ex. "United States")
    string Sponsor,   // FAI / opérateur (ex. "AT&T")
    double Lat,
    double Lon
);
