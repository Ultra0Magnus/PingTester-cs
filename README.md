# PingTester (C# / Avalonia)

Outil de **test et de surveillance de ping** pour Windows, avec interface graphique moderne :
graphique de latence en **temps réel**, surveillance **multi-hôtes**, statistiques de qualité et
**test de débit**. Port **C# / Avalonia** de la
[version Python](https://github.com/Ultra0Magnus/PingTester).

![Release](https://img.shields.io/github/v/release/Ultra0Magnus/PingTester-cs)
![Platform](https://img.shields.io/badge/plateforme-Windows-0078d4)
![.NET](https://img.shields.io/badge/.NET-10-512bd4)

## Fonctionnalités

- 📈 **Graphique de latence en direct** (une courbe par hôte), via ScottPlot.
- 🌐 **Multi-hôtes simultanés** — comparez plusieurs cibles (box, DNS, site) pour situer un
  problème (routeur vs FAI vs serveur).
- 📊 **Onglet Stats** — tableau par hôte (envoyés, perte %, min/max, moyenne, médiane, gigue) et un
  **score de qualité A-F**.
- 🚀 **Test de débit** — onglet dédié : mesure **Download + Upload** (Mbps) et **latence serveur**
  via Cloudflare (sans dépendance ajoutée), barre de progression, annulable en cours de test.
- 💾 **Préférences mémorisées** entre les sessions (hôtes, intervalle, dernier onglet) dans
  `%APPDATA%\PingTester\config.json`.

## Prérequis

- **Windows**
- **[SDK .NET 10](https://dotnet.microsoft.com/download)** (pour compiler / lancer depuis les sources)

## Lancer depuis les sources

```bash
dotnet run --project PingTester
```

1. Saisir un ou plusieurs **hôtes** séparés par des virgules (ex. `8.8.8.8, 1.1.1.1`).
2. Régler l'**intervalle** puis cliquer sur **▶ Démarrer** : la courbe se trace en direct et
   l'onglet **Stats** se met à jour.
3. Onglet **🚀 Débit** → **Tester le débit** pour mesurer download / upload / latence.

## Compiler un exécutable (.exe)

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Le binaire autonome se trouve dans
`PingTester/bin/Release/net10.0/win-x64/publish/PingTester.exe` (aucun runtime .NET requis sur la
machine cible).

## Téléchargement

Un exécutable prêt à l'emploi est disponible dans la
**[dernière release](https://github.com/Ultra0Magnus/PingTester-cs/releases/latest)**.

> ⚠️ L'exe n'est pas signé : Windows SmartScreen peut afficher un avertissement
> (*Informations complémentaires → Exécuter quand même*).

## Structure du projet

| Dossier | Rôle |
|---|---|
| `Models/` | Modèles de données (`PingSample`, `SpeedTest`, `AppPreferences`) |
| `Services/` | Ping natif, test de débit Cloudflare, persistance des préférences |
| `ViewModels/` | Logique MVVM (CommunityToolkit.Mvvm) |
| `Views/` | Interface Avalonia (`MainWindow`) |
| `Statistics/` | Calculs (médiane, gigue, score de qualité) |

## Pile technique

- **.NET 10** + **Avalonia 11.3.4** (UI multiplateforme)
- **CommunityToolkit.Mvvm** (MVVM : `[ObservableProperty]`, `[RelayCommand]`)
- **ScottPlot.Avalonia** (graphe temps réel)
- Ping natif via `System.Net.NetworkInformation.Ping` (pas de sous-processus)

## Licence

[MIT](LICENSE).
