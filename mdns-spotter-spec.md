# Specht – Technische Spezifikation

*„Klopf klopf – wer wohnt im Netzwerk?"*

**Dokumentversion:** 1.0
**Datum:** Mai 2026
**Autor:** [Auftraggeber]
**Zielgruppe:** Externe Entwickler / Auftragnehmer

---

## 1. Projektüberblick

### 1.1 Was wird gebaut?

Eine kleine, native Windows-Anwendung, die alle Geräte im lokalen Netzwerk anzeigt, die per **mDNS / DNS-SD (Bonjour / Zeroconf)** ihre Dienste bewerben. Die App läuft im **System-Tray**, öffnet beim Klick ein kompaktes Dropdown-Fenster mit der Geräteliste und bietet Detailansicht, Suche, Export und Push-Benachrichtigungen bei neuen Geräten.

### 1.2 Warum?

- **Apple** liefert auf macOS einen „Bonjour Browser", **Linux** hat `avahi-browse`.
- **Windows hat nichts Vergleichbares ab Werk.** Vorhandene Tools (Bonjour Browser von Hobbicus, Discovery von Apple) sind veraltet, hässlich oder nicht im Store verfügbar.
- Heimnutzer und Sysadmins brauchen oft ein schnelles „Wer spricht hier mDNS?" – für Drucker-Diagnose, AirPlay-Probleme, Smart-Home-Setup, Netzwerk-Audits.

### 1.3 Schwester-App: Spieglein

Der Auftraggeber betreibt bereits **Spieglein** (Microsoft Store: `9PL8FXP2VT14`, GitHub: `OstrongStudios/spieglein`), einen AirPlay-Empfänger für Windows in WinUI 3 / .NET 8. **Specht soll visuell und technisch in dieselbe Familie passen**: gleiche Design-Sprache, gleiche Architekturphilosophie, gleicher Distributionsweg (Microsoft Store + GitHub), gleiche Lizenz (GPL v3).

---

## 2. Ziele & Nicht-Ziele

### 2.1 Ziele (MVP)

1. **Alle** mDNS-Services im lokalen Netzwerk entdecken (nicht nur eine hartkodierte Liste).
2. Geräte gruppiert, gefiltert und durchsuchbar anzeigen.
3. Pro Gerät: Hostname, IPv4/IPv6, Port, Service-Typ, vollständige TXT-Records.
4. Benachrichtigungen bei neu auftauchenden Geräten (Windows Toast).
5. Export der aktuellen Liste als CSV und JSON.
6. Tray-First-Bedienung: keine Taskleisten-Präsenz im Normalbetrieb.
7. 100 % lokal, kein Tracking, keine Cloud-Komponenten.

### 2.2 Nicht-Ziele (für v1)

- Aktives „Probing" oder Senden eigener Service-Announcements.
- Bearbeiten/Steuern von gefundenen Geräten (kein AirPlay-Cast, kein Druck-Job).
- Cross-Subnet-Discovery (Unicast DNS-SD über externe DNS-Server).
- Mobile Versionen.
- Authentifizierung / Account-System.

---

## 3. Zielgruppe & Personas

### 3.1 Persona „Heimnutzer Hannes"

Hat zu Hause eine Mischung aus HomeKit-Geräten, einem Sonos, einem Brother-Drucker, einem Chromecast und einem Apple TV. Der Drucker taucht plötzlich nicht mehr in Word auf. Hannes will einfach sehen, **ob der Drucker noch im Netz lebt** – ohne Kommandozeile, ohne Router-Login.

**Bedarf:** klare Liste, große Icons, verständliche Namen, ein einziger Refresh-Button.

### 3.2 Persona „Power-User Petra"

Entwicklerin, debuggt eine eigene IoT-Anwendung, die `_myservice._tcp.local` bewirbt. Will TXT-Records prüfen, Port verifizieren, sehen ob IPv6 angekündigt wird.

**Bedarf:** vollständige Details, Copy-to-Clipboard für IPs/Ports, Service-Typ-Filter, Export für Tickets.

---

## 4. Funktionale Anforderungen

### 4.1 Discovery

| ID | Anforderung |
|----|-------------|
| F-DISC-01 | Beim App-Start `_services._dns-sd._udp.local` (PTR) abfragen, um **alle** im Netz angebotenen Service-Typen zu enumerieren. |
| F-DISC-02 | Für jeden gefundenen Service-Typ einen kontinuierlichen Browser starten (PTR-Lookup). |
| F-DISC-03 | Für jede Instanz SRV- und TXT-Records auflösen, anschließend A/AAAA für den Ziel-Hostnamen. |
| F-DISC-04 | TTLs der Records beachten; abgelaufene Einträge nach Ablauf entfernen, sofern kein Goodbye-Paket (TTL=0) zuvor kam. |
| F-DISC-05 | Goodbye-Pakete erkennen und das Gerät aus der Liste entfernen (mit kurzer „Verschwunden"-Animation). |
| F-DISC-06 | Mehrere aktive Netzwerk-Adapter parallel bedienen (Ethernet + WLAN gleichzeitig). |
| F-DISC-07 | IPv4 **und** IPv6 erfassen; in der UI beide anzeigen. |
| F-DISC-08 | Automatischer Re-Scan nach Netzwerkwechsel (`NetworkChange.NetworkAddressChanged`). |
| F-DISC-09 | Manueller „Refresh"-Button leert den Cache und startet die Discovery neu. |

### 4.2 UI / Interaktion

| ID | Anforderung |
|----|-------------|
| F-UI-01 | App startet mit Windows (optional, in Settings abschaltbar) und sitzt direkt im System-Tray. |
| F-UI-02 | Linksklick auf Tray-Icon öffnet ein Dropdown-Fenster (~420 × 640 px) direkt über dem Icon. |
| F-UI-03 | Rechtsklick öffnet Kontextmenü: *Öffnen*, *Refresh*, *Einstellungen*, *Über*, *Beenden*. |
| F-UI-04 | Hauptfenster zeigt: Header mit Suchfeld + Filter-Chips, scrollbare Geräteliste, Statusleiste unten. |
| F-UI-05 | Geräteliste-Kartenlayout: Icon (Service-Typ), Anzeigename, Hostname, IP, Service-Typ-Badge. |
| F-UI-06 | Klick auf eine Karte öffnet Detailansicht (Slide-In von rechts oder neue Page). |
| F-UI-07 | Detailansicht: alle IPs (v4+v6), Port, vollständiger Service-Typ, TXT-Records als Key-Value-Tabelle, „Kopieren"-Buttons pro Feld. |
| F-UI-08 | Suchfeld filtert über Name, Hostname, IP, Service-Typ in Echtzeit (debounced). |
| F-UI-09 | Filter-Chips für Service-Kategorien (AirPlay, Cast, Print, HomeKit, File-Share, Audio, Sonstige). |
| F-UI-10 | Neu erscheinende Geräte werden mit kurzem Highlight und Windows-Toast gemeldet (Toast abschaltbar). |
| F-UI-11 | Export als CSV und JSON aus dem Menü heraus, Speicherdialog. |
| F-UI-12 | Vollständig Tastatur-bedienbar (Tab-Reihenfolge, Enter öffnet Details, Esc schließt). |
| F-UI-13 | Mehrsprachig: Deutsch und Englisch, automatisch nach System-Locale, manuell umschaltbar. |
| F-UI-14 | Dark/Light Mode folgt Windows-Theme; manueller Override in Settings. |

### 4.3 Settings

| ID | Anforderung |
|----|-------------|
| F-SET-01 | Autostart mit Windows (an/aus). |
| F-SET-02 | Toast-Benachrichtigungen (an/aus). |
| F-SET-03 | Scan-Intervall (Standard: kontinuierlich; alternativ alle 30/60/300 Sekunden). |
| F-SET-04 | Sprache: System / Deutsch / Englisch. |
| F-SET-05 | Theme: System / Dark / Light. |
| F-SET-06 | Service-Typ-Whitelist/Blacklist (optional, ausgeklappt unter „Erweitert"). |
| F-SET-07 | Reset-Knopf für alle Einstellungen. |

### 4.4 Export-Format

**CSV-Header (UTF-8 mit BOM, Semikolon-getrennt für Excel-Kompatibilität auf DE-Systemen):**
```
Anzeigename;Hostname;IPv4;IPv6;Port;ServiceTyp;TXT
```

**JSON-Schema:**
```json
{
  "exportedAt": "2026-05-21T14:32:00+02:00",
  "deviceCount": 12,
  "devices": [
    {
      "displayName": "Wohnzimmer Apple TV",
      "hostname": "Apple-TV.local",
      "addresses": { "ipv4": ["192.168.1.42"], "ipv6": ["fe80::1"] },
      "port": 7000,
      "serviceType": "_airplay._tcp.local",
      "txt": { "deviceid": "AA:BB:CC:DD:EE:FF", "model": "AppleTV6,2" },
      "firstSeen": "2026-05-21T14:01:12+02:00",
      "lastSeen": "2026-05-21T14:31:58+02:00"
    }
  ]
}
```

---

## 5. Nicht-funktionale Anforderungen

| Kategorie | Anforderung |
|-----------|-------------|
| **Performance** | Erste sichtbare Geräte ≤ 2 s nach App-Start. Vollständige Discovery typischer Heimnetze ≤ 10 s. Idle-CPU < 1 %, idle RAM < 80 MB. |
| **Stabilität** | App muss Netzwerk-Wechsel (WLAN ↔ Ethernet, VPN auf/zu) ohne Neustart überleben. Sleep/Wake-Watchdog analog zu Spieglein. |
| **Privatsphäre** | Keine Telemetrie. Keine ausgehenden Verbindungen außerhalb des LAN. Keine Logfiles mit personenbezogenen Daten ohne explizite Opt-in-Debug-Option. |
| **Sicherheit** | Pakete strikt aus dem lokalen Subnetz akzeptieren. Keine Auswertung von Inhalten Dritter (nur Service-Metadaten). |
| **Barrierefreiheit** | Vollständige Tastaturbedienung, Screen-Reader-Labels (Narrator-kompatibel), Kontrast WCAG AA. |
| **Kompatibilität** | Windows 10 (Build 19041) und Windows 11, x64 und ARM64. |

---

## 6. Tech-Stack-Empfehlung

> **Versions-Stand:** Mai 2026. Alle Versionsangaben verifiziert. Vor Projektstart konkrete Patchstände erneut auf NuGet prüfen.

| Schicht | Empfehlung | Begründung |
|---------|------------|------------|
| **Runtime** | **.NET 10 (LTS)** | Aktuelle LTS-Version, veröffentlicht Nov 2025, Support bis Nov 2028. Spieglein läuft aktuell noch auf .NET 8 (LTS-Ende Nov 2026) – Specht direkt auf .NET 10 zu starten ist zukunftssicher und schafft zugleich einen guten Anlass, Spieglein mitzuziehen. |
| **Sprache** | **C# 14** | Wird mit .NET 10 ausgeliefert. |
| **UI-Framework** | **WinUI 3 / Windows App SDK 2.0** (Release 13.02.2026) | Erstes 2.x-Major-Release. Native Fluent-Optik, Mica, ContentIsland-API, Drag-&-Drop in WebView2. Toolchain: Visual Studio 2026 oder die neuen `dotnet new winui`-Templates (CLI). |
| **Tray** | **H.NotifyIcon.WinUI 2.4.1** | De-facto-Standard für Tray-Icons in WinUI 3, weiterhin aktiv gepflegt. |
| **mDNS-Library** | **Makaretu.Dns.Multicast.New 0.38.x** | Aktiv gepflegter Fork des ursprünglichen `richardschneider/net-mdns` (Original seit 2019 verwaist). Reines C#, MIT-Lizenz, unterstützt Service-Enumeration, IPv4 + IPv6, mehrere Netzwerk-Interfaces, Subtypes, Reverse-Address-Mapping. |
| _Alternative mDNS_ | Zeroconf 3.7.x (novotnyllc) | Cross-Platform-Bibliothek mit aktiver Pflege, andere API-Philosophie (eher Browse/Resolve auf Anfrage statt kontinuierlich). Sinnvoll, falls später eine mobile/Cross-Platform-Variante denkbar ist. |
| **DI / Hosting** | Microsoft.Extensions.Hosting | BackgroundService für Discovery, Singleton-Cache, ViewModel-Injection. |
| **Lokalisierung** | `.resw`-Ressourcendateien (Standard WinUI) | Konsistent mit Spieglein. |
| **Settings** | `Microsoft.Windows.Storage.ApplicationData` (Store-Build) bzw. JSON-Datei (Unpacked) | Funktioniert in beiden Distributionswegen. |
| **Packaging** | MSIX | Pflicht für Store, sauber für Side-Loading. |
| **Build / CI** | GitHub Actions mit windows-latest, MSIX-Signierung, automatische Release-Artefakte | Wie bei Spieglein. |
| **Tests** | xUnit + selbst gebauter Mock-mDNS-Responder (~100 LoC) | UDP-Multicast lässt sich gut mocken. |
| **AI-Tooling (optional)** | WinUI-Agent-Plugin für GitHub Copilot / Claude Code (April 2026) | Microsoft-Plugin mit 8 spezialisierten WinUI-Skills für Scaffolding, Build, Tests, Migration. Praktisch, nicht zwingend. |

### 6.1 Warum nicht Apples `dnssd.dll` (Bonjour SDK für Windows)?

- Bonjour SDK ist seit Jahren ohne Update.
- Endnutzer müssten Bonjour Print Services oder iTunes installiert haben – das verträgt sich schlecht mit Microsoft-Store-Distribution.
- Makaretu liefert vergleichbare Funktionalität in Managed Code, ohne externe Abhängigkeit.

---

## 7. Architektur

### 7.1 Komponenten

```
┌──────────────────────────────────────────────────┐
│  WinUI 3 App-Schale (App.xaml, MainWindow)       │
│  ┌────────────────────────────────────────────┐  │
│  │  Views (XAML)                              │  │
│  │   ├─ TrayDropdownPage                      │  │
│  │   ├─ DeviceDetailPage                      │  │
│  │   ├─ SettingsPage                          │  │
│  │   └─ AboutPage                             │  │
│  └─────────────┬──────────────────────────────┘  │
│                │ DataBinding                     │
│  ┌─────────────▼──────────────────────────────┐  │
│  │  ViewModels (MVVM Community Toolkit)       │  │
│  └─────────────┬──────────────────────────────┘  │
│                │                                 │
│  ┌─────────────▼──────────────────────────────┐  │
│  │  Services                                  │  │
│  │   ├─ IDiscoveryService (BackgroundService) │  │
│  │   ├─ IDeviceCache       (Singleton, Obs.)  │  │
│  │   ├─ INotificationService (Toast)          │  │
│  │   ├─ IExportService    (CSV/JSON)          │  │
│  │   └─ ISettingsService                      │  │
│  └─────────────┬──────────────────────────────┘  │
│                │                                 │
│  ┌─────────────▼──────────────────────────────┐  │
│  │  Makaretu.Dns.Multicast.New                │  │
│  │   (MulticastService + ServiceDiscovery)    │  │
│  └────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

### 7.2 Discovery-Loop (Pseudocode)

```csharp
public sealed class DiscoveryService : BackgroundService
{
    private readonly MulticastService _mdns = new();
    private readonly ServiceDiscovery _sd;
    private readonly IDeviceCache _cache;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _sd = new ServiceDiscovery(_mdns);

        _sd.ServiceInstanceDiscovered += (s, e) => OnInstanceFound(e);
        _sd.ServiceInstanceShutdown   += (s, e) => OnInstanceGone(e);
        _mdns.AnswerReceived          += (s, e) => Resolve(e);

        _mdns.Start();
        _sd.QueryAllServices(); // PTR auf _services._dns-sd._udp.local

        NetworkChange.NetworkAddressChanged += (_, __) => RestartScan();

        await Task.Delay(Timeout.Infinite, ct);
    }
    // ...
}
```

### 7.3 Datenmodell

```csharp
public sealed record Device(
    string DisplayName,        // Aus Service-Instanzname
    string Hostname,           // SRV target
    IReadOnlyList<IPAddress> Addresses,
    ushort Port,
    string ServiceType,        // z.B. "_airplay._tcp.local"
    IReadOnlyDictionary<string, string> Txt,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    ServiceCategory Category   // berechnet: AirPlay, Cast, Print, HomeKit, ...
);

public enum ServiceCategory
{
    AirPlay, Cast, Print, HomeKit, FileShare,
    Audio, RemoteControl, IoT, Other
}
```

### 7.4 Service-Typ → Kategorie Mapping (Auswahl)

| Service-Typ | Kategorie | Typische Geräte |
|-------------|-----------|------------------|
| `_airplay._tcp` | AirPlay | Apple TV, AirPlay-Lautsprecher |
| `_raop._tcp` | AirPlay | AirPlay-Audio (Remote Audio Output Protocol) |
| `_googlecast._tcp` | Cast | Chromecast, Google Nest, viele Smart-TVs |
| `_spotify-connect._tcp` | Audio | Spotify-Connect-Lautsprecher |
| `_hap._tcp` | HomeKit | HomeKit-Zubehör |
| `_homekit._tcp` | HomeKit | HomeKit (älter) |
| `_ipp._tcp` / `_ipps._tcp` | Print | IPP-Drucker |
| `_printer._tcp` | Print | LPR-Drucker |
| `_pdl-datastream._tcp` | Print | HP JetDirect |
| `_smb._tcp` | FileShare | SMB/CIFS |
| `_afpovertcp._tcp` | FileShare | AFP |
| `_nfs._tcp` | FileShare | NFS |
| `_ssh._tcp` | RemoteControl | SSH-Hosts |
| `_sftp-ssh._tcp` | RemoteControl | SFTP |
| `_workstation._tcp` | Sonstige | macOS/Linux-Hosts |
| `_device-info._tcp` | Sonstige | generische Geräte-Info |
| _alle übrigen_ | Other | – |

Die Liste lebt in einer JSON-Ressource und kann ohne Code-Änderung erweitert werden.

---

## 8. UI-Spezifikation

### 8.1 Visueller Stil

- **Backdrop:** Mica Alt (Windows 11) bzw. Acrylic (Windows 10 Fallback)
- **Akzentfarbe:** System-Akzent, mit kategorischen Sekundärfarben für Service-Badges
- **Typografie:** Segoe UI Variable (Windows 11) / Segoe UI (10)
- **Icons:** Segoe Fluent Icons, ergänzt durch eigene SVGs für Service-Kategorien
- **Eckenradius:** 4 / 8 px gemäß Fluent
- **Spacing:** 12 / 16 / 24 px Raster

### 8.2 Layout Hauptfenster (420 × 640 px)

```
┌──────────────────────────────────────────┐
│  🔍 [ Suche... ]            ⟳ ☰          │  ← Header 56 px
├──────────────────────────────────────────┤
│  [Alle] [AirPlay] [Print] [Cast] [+]    │  ← Filter-Chips 44 px
├──────────────────────────────────────────┤
│                                          │
│  ┌────────────────────────────────────┐  │
│  │ 📺  Wohnzimmer Apple TV       •›   │  │  ← Karte
│  │     Apple-TV.local · AirPlay        │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ 🖨️  HP LaserJet Pro            •›  │  │
│  │     192.168.1.50 · IPP              │  │
│  └────────────────────────────────────┘  │
│  ...                                     │
│                                          │
├──────────────────────────────────────────┤
│  12 Geräte · zuletzt aktualisiert 14:32  │  ← Statusleiste 32 px
└──────────────────────────────────────────┘
```

### 8.3 Detailansicht

- Zurück-Pfeil oben links
- Großes Icon + Anzeigename + Service-Kategorie-Badge
- Sektionen:
  1. **Verbindung** – IPv4, IPv6, Port (jeweils mit Copy-Button)
  2. **Identität** – vollständiger Service-Typ, Service-Instanzname, Hostname
  3. **TXT-Records** – Tabelle Key/Value, scrollbar, monospaced font für Werte
  4. **Aktivität** – Erstmals gesehen, zuletzt gesehen
- Footer-Button: „Als JSON kopieren"

---

## 9. Edge Cases & Fehlerbehandlung

| Fall | Verhalten |
|------|-----------|
| Kein Netzwerk vorhanden | Empty State mit Icon + „Keine Netzwerkverbindung" |
| Firewall blockiert UDP 5353 | Beim Start prüfen; bei 0 Antworten nach 5 s Hinweis-Banner mit Link zu FAQ |
| Doppelte Service-Instanzen (gleicher Name, unterschiedliche Adapter) | In der UI mergen; intern beide Adapter-Quellen behalten |
| Sehr großes Netz (>100 Geräte) | Virtualisierte Liste (`ItemsRepeater`), Lazy-Loading der TXT-Details |
| Goodbye-Paket ohne vorherige Discovery | Ignorieren |
| Sleep/Wake | Watchdog erkennt `PowerModeChanged`, Discovery wird sauber neu aufgesetzt |
| Mehrere Default-Interfaces | Alle aktiven Interfaces parallel beobachten; in der Detailansicht Quelle anzeigen |
| Unicode in Service-Instanznamen | UTF-8 sauber dekodieren, RTL-Texte korrekt rendern |
| TXT-Werte mit Binär-Bytes | Hex-Darstellung mit Hinweis-Icon |

---

## 10. Akzeptanzkriterien (Definition of Done)

1. App startet auf Windows 10 (19041) und Windows 11 (x64 + ARM64) ohne Fehlermeldung.
2. In einem typischen Heimnetz mit Apple TV, HomePod, Chromecast, AirPrint-Drucker werden **alle vier** innerhalb von 10 s sichtbar.
3. Suche filtert verzögerungsfrei (< 100 ms wahrgenommene Latenz) bei 200 Geräten.
4. Toast-Benachrichtigung erscheint genau einmal pro neu auftauchendem Gerät.
5. CSV-Export öffnet sich in Excel auf einem deutschen Windows ohne Spaltenverschiebung.
6. App überlebt einen kompletten Sleep/Wake-Zyklus und einen WLAN-Wechsel.
7. Idle-CPU < 1 %, RAM < 80 MB nach 1 h Laufzeit.
8. Vollständige DE- und EN-Übersetzung, keine hardgecodeten Strings im UI.
9. MSIX-Paket signiert, im Microsoft Store einreichbar.
10. Quellcode auf GitHub unter GPL v3, mit README, Build-Anleitung und Architektur-Diagramm.

---

## 11. Lieferumfang

- Quellcode-Repository (GitHub, Auftraggeber-Org)
- MSIX-Paket, signiert
- Microsoft-Store-Submission-Assets (Screenshots in DE/EN, Beschreibungstexte)
- README.md, CONTRIBUTING.md, LICENSE (GPL v3)
- Kurze Architektur-Dokumentation in `/docs`
- GitHub-Actions-Workflow für Build und Release

---

## 12. Roadmap / Milestones

| Milestone | Inhalt | Aufwand (grob) |
|-----------|--------|----------------|
| **M1 – Discovery-Kern** | mDNS-Library angebunden, Service-Enumeration, Konsolen-Testprogramm zeigt alle Geräte | ~3 PT |
| **M2 – UI-Grundgerüst** | WinUI-3-App mit Tray, Hauptliste, Suche, Filter, Dark/Light | ~5 PT |
| **M3 – Detailansicht + Export** | Detail-Page, TXT-Tabelle, CSV/JSON-Export | ~2 PT |
| **M4 – Polish** | Lokalisierung DE/EN, Settings, Toast-Benachrichtigungen, Watchdog | ~3 PT |
| **M5 – Tests + Stabilisierung** | Unit-Tests, Mock-Responder, Netzwerk-Wechsel-Szenarien | ~2 PT |
| **M6 – Release** | MSIX-Signierung, Store-Submission, GitHub-Release | ~1 PT |
| **Gesamt** | | **~16 Personentage** |

---

## 13. Lizenz & Open Source

- **Lizenz:** GPL v3 (analog zu Spieglein)
- **Repository:** öffentlich auf GitHub
- **Drittsoftware-Hinweise** im About-Dialog:
  - Makaretu.Dns.Multicast.New (MIT)
  - H.NotifyIcon.WinUI (MIT)
  - .NET 10 / WinUI 3 / Windows App SDK 2.0 (MIT)
- **Markenhinweis:** „Nicht von Apple Inc. Bonjour ist eine eingetragene Marke von Apple Inc."

---

## 14. Optionale Erweiterungen (Post-MVP)

- **„Spieglein erkannt"-Hinweis:** Wenn ein anderer Spieglein-Host im Netz gefunden wird, dezenter Hinweis-Badge.
- **Geräte-Verlauf:** Lokale Historie (SQLite), „Wann war Gerät X zuletzt online?"
- **Service-Test:** Klick auf Port → Mini-TCP-Probe, „Port erreichbar?"
- **Markieren / Favoriten:** Eigene Anzeigenamen für wichtige Geräte.
- **CLI-Companion:** `specht.exe --json` für Skripting.

---

## 15. Kontakt

**Auftraggeber:** Ostrong Studios
**Web:** https://www.ostrongstudios.de
**Referenzprojekt:** Spieglein – https://github.com/OstrongStudios/spieglein
