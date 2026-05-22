# Specht

*„Klopf klopf — wer wohnt im Netzwerk?"*

Specht ist eine schlanke Windows-Tray-App, die dir auf einen Blick zeigt, welche Geräte gerade in deinem lokalen Netzwerk erreichbar sind — Drucker, Smart-TVs, AirPlay-Lautsprecher, HomeKit-Zubehör, NAS, Streaming-Empfänger und alles andere, das **mDNS / DNS-SD (Bonjour / Zeroconf)** spricht.

Klick aufs Tray-Icon, sieh die Liste, klick auf ein Gerät für Details (IPs, Port, Service-Typ, TXT-Records). Fertig.

## Features

- 🐦 **Tray-First** — sitzt unauffällig im Benachrichtigungsbereich, öffnet sich auf Klick als kompaktes Dropdown
- 🔍 **Suche + Kategorie-Filter** — AirPlay, Cast, Print, HomeKit, FileShare, IoT, Audio, …
- 📋 **Detailansicht** — alle IPv4-/IPv6-Adressen, Port, vollständiger Service-Typ, TXT-Records, Erstmals-/Letztmals-Gesehen-Zeitstempel
- 📤 **Export** — Geräteliste als CSV (Excel-DE-kompatibel) oder JSON
- 🔔 **Toast bei neuen Geräten** — optional aktivierbar
- 🌍 **Deutsch & Englisch** — folgt dem System oder manuell wählbar
- 🌓 **Dark- & Light-Mode** — folgt Windows-Theme oder manuell
- 🔒 **100 % lokal** — keine Telemetrie, keine Cloud, kein Konto. Datenschutz: siehe [docs/PRIVACY.de.md](docs/PRIVACY.de.md)

## Installation

Über den **[Microsoft Store](https://www.microsoft.com/store/apps/9P2P2WNW8WWD)** *(Link wird aktiv, sobald die App live ist)*.

## Anforderungen

Windows 10 (ab Build 19041) oder Windows 11, x64 oder ARM64.

## Schwester-App

Specht gehört zur selben Familie wie **[Spieglein](https://www.microsoft.com/store/apps/9PL8FXP2VT14)** — ein AirPlay-Empfänger für Windows. Wenn dich der Spieglein-Stil anspricht, fühlst du dich auch in Specht zuhause.

## Lizenz

Specht ist **freie Software unter der [GNU General Public License v3](LICENSE)**. Quellcode öffentlich, dauerhaft.

Verwendete Open-Source-Komponenten und ihre Lizenzen: siehe [docs/THIRD_PARTY_LICENSES.md](docs/THIRD_PARTY_LICENSES.md).

*Nicht von Apple Inc. Bonjour, AirPlay und HomeKit sind eingetragene Marken von Apple Inc. Chromecast ist eine Marke von Google LLC.*

## Mitwirken

Build aus dem Quellcode, Tests laufen lassen, Pull Requests: siehe [docs/BUILDING.md](docs/BUILDING.md).

## Herausgeber

[Ostrong Studios](https://www.ostrongstudios.de) — Mathias Oysmüller, Österreich.
