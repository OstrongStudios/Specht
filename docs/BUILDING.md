# Build & Mitwirken

Diese Datei beschreibt, wie du Specht aus dem Quellcode baust, die Tests laufen lässt und ein MSIX-Paket erzeugst. Für die reine Nutzung der App siehe [README](../README.md) — Specht gibt's fertig signiert im Microsoft Store.

## Voraussetzungen

- **Windows 10** (Build 19041+) oder **Windows 11**
- **Visual Studio 2022** (17.10+) oder **Visual Studio 2026** mit den folgenden Workloads:
  - „Desktopentwicklung mit .NET"
  - „Windows-App-Entwicklung"
- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))

Alternativ pur über die CLI ohne Visual Studio — `dotnet build` reicht für Bibliothek und unpackaged-EXE; für MSIX-Builds wird die WindowsAppSDK-Workload zusätzlich benötigt.

## Quellcode klonen

```powershell
git clone https://github.com/OstrongStudios/Specht.git
cd Specht
```

## In Visual Studio

1. `src/Specht.sln` öffnen
2. Konfiguration **Debug | x64** wählen
3. **F5** — App startet im unpackaged-Modus, Tray-Icon erscheint

## Über die CLI (unpackaged, schneller Dev-Modus)

```powershell
cd src
dotnet build Specht.App\Specht.App.csproj -c Debug -p:Platform=x64
dotnet publish Specht.App\Specht.App.csproj -c Debug -r win-x64 --self-contained=false -p:WindowsAppSDKSelfContained=true
.\Specht.App\bin\Debug\net10.0-windows10.0.19041.0\win-x64\publish\Specht.exe
```

## Tests laufen lassen

51 Unit- und Integrationstests für die Discovery-, Cache-, Export- und Settings-Logik:

```powershell
dotnet test tests\Specht.Core.Tests\Specht.Core.Tests.csproj
```

Oder über den Test-Explorer in Visual Studio (`Strg + E, T`).

## MSIX-Paket bauen

### Für den Microsoft Store

```powershell
cd src
dotnet build Specht.App\Specht.App.csproj `
  -c Release -p:Platform=x64 `
  -p:WindowsPackageType=MSIX `
  -p:AppxBundle=Always `
  -p:AppxBundlePlatforms="x64|arm64" `
  -p:UapAppxPackageBuildMode=StoreUpload `
  -p:AppxPackageSigningEnabled=false `
  -p:AppxSymbolPackageEnabled=false
```

Output:
`src/Specht.App/AppPackages/Specht.App_<version>_x64_arm64_bundle.msixupload`

Diese `.msixupload`-Datei wird im [Microsoft Partner Center](https://partner.microsoft.com/dashboard) als neues Paket hochgeladen. Signierung erfolgt server-seitig durch Microsoft.

### Für lokales Testen (unsigniert)

```powershell
cd src
dotnet build Specht.App\Specht.App.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=MSIX -p:AppxPackageSigningEnabled=false
```

Installation des unsignierten MSIX setzt voraus:

1. **Entwicklermodus** aktivieren: Einstellungen → Datenschutz & Sicherheit → Für Entwickler → Entwicklermodus an
2. ```powershell
   Add-AppxPackage -Path "src\Specht.App\AppPackages\Specht.App_1.0.0.0_Test\Specht.App_1.0.0.0_x64.msix"
   ```

### Mit selbst-erstelltem Zertifikat (für Verteilung außerhalb des Stores)

```powershell
$cert = New-SelfSignedCertificate -Type Custom `
  -Subject "CN=Ostrong Studios" `
  -KeyUsage DigitalSignature `
  -FriendlyName "Specht-Sign" `
  -CertStoreLocation "Cert:\CurrentUser\My"

$pwd = ConvertTo-SecureString -String "deinpasswort" -Force -AsPlainText

Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" `
  -FilePath "specht.pfx" -Password $pwd

cd src
dotnet build Specht.App\Specht.App.csproj `
  -c Release -p:Platform=x64 -p:WindowsPackageType=MSIX `
  -p:PackageCertificateKeyFile=..\specht.pfx `
  -p:PackageCertificatePassword=deinpasswort
```

Auf dem Zielrechner muss das öffentliche Zertifikat ins „Vertrauenswürdige Stammzertifizierungsstellen"-Store importiert werden, damit das MSIX akzeptiert wird.

## Projektstruktur

```
Specht/
├── src/
│   ├── Specht.sln
│   ├── Specht.Core/           Datenmodell + Discovery (.NET 10 Klassenbibliothek)
│   │   ├── Models/            Device, ServiceCategory
│   │   ├── Services/          DiscoveryService, DeviceCache, ExportService, SettingsService
│   │   ├── Resources/         service-categories.json (mDNS-Typ → Kategorie-Mapping)
│   │   ├── NetworkUtils.cs    VPN-Detection, Subnet-Match
│   │   └── ServiceTypeMapping.cs
│   └── Specht.App/            WinUI 3 Tray-App (.NET 10)
│       ├── App.xaml(.cs)      App-Lifecycle + Tray-Icon
│       ├── Program.cs         Custom Main mit Single-Instance via AppInstance
│       ├── MainWindow.xaml    Tray-Dropdown 420×640, Mica-Backdrop
│       ├── Pages/             MainPage, DeviceDetailPage, SettingsPage, AboutPage
│       ├── ViewModels/        MVVM (CommunityToolkit)
│       ├── Services/          AutostartService, ToastService, PowerWatchdog
│       ├── Localization/      TranslateExtension (XAML Markup Extension)
│       ├── Strings/           Resw-Ressourcen DE + EN
│       ├── Assets/            Icons, MSIX-Tile-Bilder
│       └── Package.appxmanifest
├── tests/
│   └── Specht.Core.Tests/     xUnit, 51 Tests
├── scripts/
│   └── generate-icons.ps1     SVG → alle MSIX-PNG-Varianten + Multi-Res-ICO
├── docs/
│   ├── PRIVACY.de.md          Datenschutzerklärung
│   ├── BUILDING.md            diese Datei
│   └── THIRD_PARTY_LICENSES.md
└── spike/                     Erste Library-Validierung (Throwaway)
```

## Icons regenerieren

Wenn du das App-Icon änderst (`src/Specht.App/Assets/source/specht.svg`):

```powershell
.\scripts\generate-icons.ps1
```

Voraussetzung: ImageMagick (`winget install ImageMagick.ImageMagick`).

## Pull Requests

Diskussionen und Beiträge sind willkommen. Bitte:

1. Vor dem Commit Tests laufen lassen (`dotnet test`)
2. XAML-Strings nicht hartcodieren — über `{loc:Translate Key=...}` oder `Strings.Get(...)` in den resw-Files lokalisieren
3. Architekturentscheidungen, die das Spec-Verhalten ändern, vorab in einem Issue diskutieren

## Lizenz

Specht ist [GPL v3](../LICENSE). Beiträge sind unter derselben Lizenz akzeptiert.
