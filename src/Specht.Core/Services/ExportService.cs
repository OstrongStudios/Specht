using System.Globalization;
using System.Text;
using System.Text.Json;
using Specht.Core.Models;

namespace Specht.Core.Services;

public sealed class ExportService : IExportService
{
    public string ToCsv(IEnumerable<Device> devices)
    {
        var sb = new StringBuilder();
        sb.Append('﻿'); // UTF-8 BOM for Excel
        sb.AppendLine("Anzeigename;Hostname;IPv4;IPv6;Port;ServiceTyp;TXT");
        foreach (var d in devices)
        {
            sb.Append(Esc(d.DisplayName)).Append(';');
            sb.Append(Esc(d.Hostname ?? "")).Append(';');
            sb.Append(Esc(string.Join("|", d.IPv4.Select(a => a.ToString())))).Append(';');
            sb.Append(Esc(string.Join("|", d.IPv6.Select(a => a.ToString())))).Append(';');
            sb.Append(d.Port?.ToString(CultureInfo.InvariantCulture) ?? "").Append(';');
            sb.Append(Esc(d.ServiceType)).Append(';');
            sb.Append(Esc(string.Join("|", d.Txt.Select(t => $"{t.Key}={t.Value}"))));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public string ToJson(IEnumerable<Device> devices)
    {
        var snapshot = devices.ToArray();
        var payload = new
        {
            exportedAt = DateTimeOffset.Now,
            deviceCount = snapshot.Length,
            devices = snapshot.Select(d => new
            {
                displayName = d.DisplayName,
                hostname = d.Hostname,
                addresses = new
                {
                    ipv4 = d.IPv4.Select(a => a.ToString()).ToArray(),
                    ipv6 = d.IPv6.Select(a => a.ToString()).ToArray(),
                },
                port = d.Port,
                serviceType = d.ServiceType,
                txt = d.Txt,
                firstSeen = d.FirstSeen,
                lastSeen = d.LastSeen,
            }).ToArray(),
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    private static string Esc(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var needsQuote = s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        if (!needsQuote) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
