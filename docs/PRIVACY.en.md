# Privacy Policy for the "Specht" Application

*As of: 17 May 2026*

## 1. Controller

**Ostrong Studios** (owner: Mathias Oysmüller)
Altwaldhäusl 55
3662 Münichreith-Laimbach
Lower Austria, Austria

E-mail: support@ostrongstudios.de
Phone: +43 7413 22341
Web: https://ostrongstudios.de

## 2. Purpose and operation of the application

"Specht" is a Windows application that discovers devices on the local network via **mDNS / DNS-SD (Bonjour / Zeroconf)** and shows them in a list. The app **sends active mDNS queries to the local network** (multicast packets on UDP 5353) and receives the responses of reachable devices (printers, smart TVs, HomeKit accessories, NAS, streaming receivers, and the like). From those responses it displays hostnames, IP addresses, service types, and metadata.

All communication takes place **exclusively locally within the same WLAN/LAN** between your PC and the other devices on your network. No data transfer to external servers occurs.

## 3. Processed data

When you use Specht, the following data are processed **exclusively locally on your device and without any transmission to us or to third parties**:

| Data | Location | Purpose |
|------|----------|---------|
| Configuration (theme, language, autostart, toast notifications, hidden categories) | `%LOCALAPPDATA%\Packages\4663Ostronggames.Specht_e5a5qvsqnd7j6\LocalCache\Local\Specht\settings.json` | Restoring your settings on the next launch |
| Hostnames, IPv4/IPv6 addresses, service types, ports, and TXT records of devices reachable on the LAN | Volatile, in RAM | Display of the device list while the app is running |
| Timestamps of first and last sighting per device | Volatile, in RAM | Display in the detail view |
| Manually exported lists (CSV/JSON) | Path of your choosing | Only on your active save action |
| Autostart entry (only if you enable it in settings) | `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run\Specht` (Windows registry) | Auto-launching with Windows. Content is solely the program path. Removed again when you disable it. |
| Notification content for new devices (only if you enable toast notifications in settings) | Handed off to the Windows notification API (Action Center) | Displaying a toast notification. The hand-off is in-process to Windows; no external transmission. |

**None of this data leaves your PC.** No transmission to Ostrong Studios, Apple Inc., Microsoft Corporation, or any third party occurs.

## 4. Network communication

While running, Specht uses exclusively local network connections:

- mDNS / Bonjour (UDP 5353, multicast addresses 224.0.0.251 for IPv4 and ff02::fb for IPv6) — both for sending discovery queries and for receiving responses from other devices

These packets stay within the local subnet and contain no personally identifying tokens.

Specht does **not** actively establish any internet connection. Updates of the application happen solely via the Microsoft Store mechanism; the [Microsoft Privacy Statement](https://privacy.microsoft.com/en-us/privacystatement) applies to that.

## 5. Cookies, tracking, analytics

Specht uses **no cookies, no tracking, no analytics or telemetry tools** (no Google Analytics, no Firebase, no App Center, no crash reports to third parties).

## 6. Third-party components (executed locally)

Specht integrates the following open-source components, which also run locally on your PC and do not establish connections to external servers:

- **Makaretu.Dns.Multicast.New** — MIT — https://github.com/makaretu/net-mdns
- **H.NotifyIcon.WinUI** — MIT — https://github.com/HavenDV/H.NotifyIcon
- **CommunityToolkit.Mvvm** — MIT — https://github.com/CommunityToolkit/dotnet
- **Microsoft Windows App SDK / WinUI 3 / .NET 10** — MIT — https://github.com/microsoft/WindowsAppSDK

The source code of Specht itself is publicly available under GPL v3: https://github.com/OstrongStudios/Specht

## 7. Legal basis

Because no personal data are transmitted to Ostrong Studios or processed by us through Specht, no data-protection-relevant processing on our part takes place.

If you actively contact us yourself (e.g. a support request to `support@ostrongstudios.de`), the processing of your request and contact data is based on your consent (Art. 6 (1) (a) GDPR) or on the steps prior to entering into a contract (Art. 6 (1) (b) GDPR). We retain your request only as long as required to handle it, at most 3 years.

## 8. Your rights as a data subject

You have at any time the right to:

- Access to your stored data (Art. 15 GDPR)
- Rectification of incorrect data (Art. 16)
- Erasure ("right to be forgotten", Art. 17)
- Restriction of processing (Art. 18)
- Data portability (Art. 20)
- Objection to processing (Art. 21)
- Withdrawal of a previously granted consent (Art. 7 (3))

To exercise your rights, please contact us informally at: support@ostrongstudios.de

## 9. Right to complain to the supervisory authority

You have the right to lodge a complaint with the Austrian data-protection authority:

**Österreichische Datenschutzbehörde**
Barichgasse 40–42, 1030 Vienna, Austria
Phone: +43 1 52 152-0
E-mail: dsb@dsb.gv.at
Web: https://www.dsb.gv.at

## 10. Version and changes

This privacy policy is valid from **17 May 2026**. We reserve the right to adapt this policy in response to changes to the application or to legal requirements. The current version is available via the Microsoft Store listing for Specht and in the public source-code repository at https://github.com/OstrongStudios/Specht (file `docs/PRIVACY.en.md`).

---

© 2026 Ostrong Studios.
