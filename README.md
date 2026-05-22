# Specht

*„Klopf klopf – wer wohnt im Netzwerk?"*

Native Windows-Tray-App, die alle Geräte im lokalen Netzwerk per **mDNS / DNS-SD (Bonjour / Zeroconf)** anzeigt. Schwesterprojekt zu [Spieglein](https://github.com/OstrongStudios/spieglein) — gleiche Design-Sprache, gleiche Tech-Familie.

## Status

Frühe Entwicklung. M1 (Discovery-Kern) und Grundgerüst M2 (UI-Shell mit Tray + Hauptliste) stehen. Detailansicht, Settings, Toast-Notifications, Export und Lokalisierung folgen.

## Tech-Stack

- **.NET 10** (LTS, Nov 2025 → Nov 2028)
- **WinUI 3 / Windows App SDK 2.0**
- **Makaretu.Dns.Multicast.New** für mDNS (MIT)
- **H.NotifyIcon.WinUI** für Tray-Integration (MIT)
- **CommunityToolkit.Mvvm** für MVVM-Boilerplate (MIT)

## Build

### Voraussetzungen
- Visual Studio 2022 (17.10+) oder Visual Studio 2026
- Workload „Windows App SDK C# Templates" (über Visual Studio Installer)
- .NET 10 SDK
- Windows 10 Build 19041+ oder Windows 11

### In Visual Studio
1. `src/Specht.sln` öffnen
2. Konfiguration `Debug | x64` wählen
3. F5

### Via CLI (unpackaged, schneller Dev-Modus)
```powershell
cd src
dotnet build Specht.App\Specht.App.csproj -c Debug -p:Platform=x64
dotnet publish Specht.App\Specht.App.csproj -c Debug -r win-x64 --self-contained=false -p:WindowsAppSDKSelfContained=true
.\Specht.App\bin\Debug\net10.0-windows10.0.19041.0\win-x64\publish\Specht.exe
```

### MSIX-Paket bauen (Store-Modus)
```powershell
cd src
dotnet build Specht.App\Specht.App.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=MSIX -p:AppxPackageSigningEnabled=false
```
Output: `src/Specht.App/AppPackages/Specht.App_<version>_Test/Specht.App_<version>_x64.msix`

Zum **lokalen Installieren** des unsignierten MSIX:
1. Im **Windows Update**: Entwicklermodus aktivieren (Einstellungen → Datenschutz & Sicherheit → Für Entwickler → Entwicklermodus an)
2. `Add-AppxPackage -Path "...\Specht.App_1.0.0.0_x64.msix"` in PowerShell

Zum **signieren** mit selbst-erstelltem Zertifikat (für Verteilung):
```powershell
$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=Ostronggames" -KeyUsage DigitalSignature -FriendlyName "Specht-Sign" -CertStoreLocation "Cert:\CurrentUser\My"
$pwd = ConvertTo-SecureString -String "specht" -Force -AsPlainText
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath "specht.pfx" -Password $pwd
# Im Build dann:
dotnet build Specht.App\Specht.App.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=MSIX -p:PackageCertificateKeyFile=..\specht.pfx -p:PackageCertificatePassword=specht
```

Für die **Store-Submission** den VS-Wizard nutzen: Rechtsklick auf Specht.App → Veröffentlichen → Microsoft Store. Identity Name/Publisher werden dabei vom Partner Center automatisch übernommen.

## Projektstruktur

```
Specht/
├── mdns-spotter-spec.md       Pflichtenheft (Auftraggeber-Doku)
├── spike/                     Erste Library-Validierung (Throwaway)
├── src/
│   ├── Specht.sln
│   ├── Specht.Core/           Datenmodell + Discovery (.NET 10 Klassenbibliothek)
│   │   ├── Models/            Device, ServiceCategory
│   │   ├── Services/          DiscoveryService, DeviceCache
│   │   └── ServiceTypeMapping.cs
│   └── Specht.App/            WinUI 3 Tray-App (.NET 10)
│       ├── App.xaml(.cs)      App-Lifecycle + Tray-Icon
│       ├── MainWindow.xaml    Tray-Dropdown 420×640
│       └── ViewModels/        MVVM (Toolkit)
└── docs/
    └── THIRD_PARTY_LICENSES.md
```

## Lizenz

**GPL v3** — siehe [LICENSE](LICENSE).

Drittsoftware: siehe [docs/THIRD_PARTY_LICENSES.md](docs/THIRD_PARTY_LICENSES.md).

*„Nicht von Apple Inc. Bonjour ist eine eingetragene Marke von Apple Inc."*

## Auftraggeber

[Ostrong Studios](https://www.ostrongstudios.de)
