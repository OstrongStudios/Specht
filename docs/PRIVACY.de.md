# Datenschutzerklärung für die Anwendung „Specht"

*Stand: 17. Mai 2026*

## 1. Verantwortlicher

**Ostrong Studios** (Inhaber: Mathias Oysmüller)
Altwaldhäusl 55
3662 Münichreith-Laimbach
Niederösterreich, Österreich

E-Mail: support@ostrongstudios.de
Telefon: +43 7413 22341
Web: https://ostrongstudios.de

## 2. Zweck und Funktionsweise der Anwendung

„Specht" ist eine Windows-Anwendung, die Geräte im lokalen Netzwerk per **mDNS / DNS-SD (Bonjour / Zeroconf)** erkennt und in einer Liste anzeigt. Die App lauscht passiv auf Multicast-Antworten anderer Geräte (Drucker, Smart-TVs, HomeKit-Zubehör, NAS, Streaming-Empfänger u.ä.) und stellt deren Hostnamen, IP-Adressen, Service-Typen und Metadaten dar.

Die gesamte Kommunikation erfolgt **ausschließlich lokal im selben WLAN/LAN** zwischen Ihrem PC und den anderen Geräten in Ihrem Netzwerk. Eine Übertragung von Daten an externe Server findet **nicht** statt.

## 3. Verarbeitete Daten

Bei der Nutzung von Specht werden folgende Daten verarbeitet, **ausschließlich lokal auf Ihrem Gerät und ohne Übermittlung an uns oder Dritte**:

| Datenart | Speicherort | Zweck |
|----------|-------------|-------|
| Konfiguration (Theme, Sprache, Autostart, Toast-Notifications, ausgeblendete Kategorien) | `%LOCALAPPDATA%\Packages\4663Ostronggames.Specht_e5a5qvsqnd7j6\LocalCache\Local\Specht\settings.json` | Wiederherstellung Ihrer Einstellungen beim nächsten Programmstart |
| Hostnamen, IPv4-/IPv6-Adressen, Service-Typen, Ports und TXT-Records der im LAN erreichbaren Geräte | flüchtig im Arbeitsspeicher | Anzeige der Geräteliste während der Laufzeit |
| Zeitstempel der ersten und letzten Sichtung pro Gerät | flüchtig im Arbeitsspeicher | Anzeige in der Detailansicht |
| Manuell exportierte Listen (CSV/JSON) | von Ihnen gewählter Pfad | Ausschließlich auf Ihre aktive Speichern-Aktion hin |

**Keine dieser Daten verlassen Ihren PC.** Es findet keine Übermittlung an Ostrong Studios, Apple Inc., Microsoft Corporation oder Dritte statt.

## 4. Netzwerk-Kommunikation

Specht nutzt im Betrieb ausschließlich lokale Netzwerkverbindungen:

- mDNS / Bonjour (UDP 5353, Multicast-Adressen 224.0.0.251 für IPv4 bzw. ff02::fb für IPv6) — sowohl zum Aussenden von Such-Anfragen als auch zum Empfangen der Antworten anderer Geräte

Diese Pakete verbleiben im lokalen Subnetz und enthalten keine personenbezogenen Identifikatoren.

Eine **Internet-Verbindung wird durch Specht nicht aktiv aufgebaut**. Updates der Anwendung erfolgen ausschließlich über den Microsoft-Store-Mechanismus; dafür gilt die [Datenschutzerklärung von Microsoft](https://privacy.microsoft.com/de-de/privacystatement).

## 5. Cookies, Tracking, Analyse-Tools

Specht verwendet **keine Cookies, kein Tracking, keine Analyse- oder Telemetrie-Tools** (kein Google Analytics, kein Firebase, kein App Center, keine Crash-Reports an Dritte).

## 6. Drittanbieter-Komponenten (lokal ausgeführt)

Specht integriert folgende Open-Source-Komponenten, die ebenfalls lokal auf Ihrem PC ausgeführt werden und keine Verbindung zu externen Servern aufbauen:

- **Makaretu.Dns.Multicast.New** — MIT — https://github.com/makaretu/net-mdns
- **H.NotifyIcon.WinUI** — MIT — https://github.com/HavenDV/H.NotifyIcon
- **CommunityToolkit.Mvvm** — MIT — https://github.com/CommunityToolkit/dotnet
- **Microsoft Windows App SDK / WinUI 3 / .NET 10** — MIT — https://github.com/microsoft/WindowsAppSDK

Der Quellcode von Specht selbst steht unter GPL v3 öffentlich zur Verfügung: https://github.com/OstrongStudios/Specht

## 7. Rechtsgrundlage

Da durch Specht keine personenbezogenen Daten an Ostrong Studios übermittelt oder dort verarbeitet werden, findet keine datenschutzrechtlich relevante Verarbeitung durch uns statt.

Sollten Sie uns selbst aktiv kontaktieren (z. B. Support-Anfrage an `support@ostrongstudios.de`), erfolgt die Verarbeitung Ihrer Anfrage- und Kontaktdaten auf Grundlage Ihrer Einwilligung (Art. 6 Abs. 1 lit. a DSGVO) bzw. zur Vertragsanbahnung (Art. 6 Abs. 1 lit. b DSGVO). Wir speichern Ihre Anfrage nur so lange, wie zur Bearbeitung erforderlich, längstens 3 Jahre.

## 8. Ihre Rechte als betroffene Person

Sie haben jederzeit das Recht auf:

- Auskunft über Ihre gespeicherten Daten (Art. 15 DSGVO)
- Berichtigung unrichtiger Daten (Art. 16)
- Löschung („Recht auf Vergessenwerden", Art. 17)
- Einschränkung der Verarbeitung (Art. 18)
- Datenübertragbarkeit (Art. 20)
- Widerspruch gegen die Verarbeitung (Art. 21)
- Widerruf einer erteilten Einwilligung (Art. 7 Abs. 3)

Zur Ausübung Ihrer Rechte wenden Sie sich bitte formlos an: support@ostrongstudios.de

## 9. Beschwerderecht bei der Aufsichtsbehörde

Sie haben das Recht zur Beschwerde bei der österreichischen Datenschutzbehörde:

**Österreichische Datenschutzbehörde**
Barichgasse 40–42, 1030 Wien
Telefon: +43 1 52 152-0
E-Mail: dsb@dsb.gv.at
Web: https://www.dsb.gv.at

## 10. Stand und Änderungen

Diese Datenschutzerklärung ist gültig ab dem **17. Mai 2026**. Bei Anpassungen der Anwendung oder bei Änderungen gesetzlicher Vorgaben behalten wir uns vor, diese Erklärung anzupassen. Die jeweils aktuelle Fassung ist unter https://ostrongstudios.de/datenschutzerklaerung/ einsehbar.

---

© 2026 Ostrong Studios.
