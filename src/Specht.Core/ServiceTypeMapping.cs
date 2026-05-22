using System.Reflection;
using System.Text.Json;
using Specht.Core.Models;

namespace Specht.Core;

/// <summary>
/// Maps mDNS service types to <see cref="ServiceCategory"/>.
/// The mapping is loaded from the embedded resource
/// <c>Specht.Core/Resources/service-categories.json</c>. Anything missing
/// falls back to <see cref="ServiceCategory.Other"/>.
///
/// Per spec §7.4 the list lives in a JSON resource and can be extended without
/// touching code (you still have to rebuild Core, but you don't have to touch C#).
/// </summary>
public static class ServiceTypeMapping
{
    private static readonly Dictionary<string, ServiceCategory> Map = Load();

    public static ServiceCategory Categorize(string serviceType)
    {
        var trimmed = Normalize(serviceType);
        return Map.TryGetValue(trimmed, out var cat) ? cat : ServiceCategory.Other;
    }

    public static IReadOnlyDictionary<string, ServiceCategory> All => Map;

    private static string Normalize(string serviceType)
    {
        var t = serviceType.TrimEnd('.');
        if (t.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            t = t[..^".local".Length];
        return t;
    }

    private static Dictionary<string, ServiceCategory> Load()
    {
        var result = new Dictionary<string, ServiceCategory>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var asm = typeof(ServiceTypeMapping).Assembly;
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("service-categories.json", StringComparison.OrdinalIgnoreCase));
            if (resourceName is null) return result;

            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null) return result;

            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("mappings", out var mappings)) return result;

            foreach (var prop in mappings.EnumerateObject())
            {
                var categoryStr = prop.Value.GetString();
                if (categoryStr is null) continue;
                if (Enum.TryParse<ServiceCategory>(categoryStr, ignoreCase: true, out var category))
                    result[prop.Name] = category;
            }
        }
        catch
        {
            // Fall back to empty map → everything is Other.
        }
        return result;
    }
}
