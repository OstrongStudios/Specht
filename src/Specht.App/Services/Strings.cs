using Microsoft.Windows.ApplicationModel.Resources;
using Specht.Core.Services;

namespace Specht.App.Services;

/// <summary>
/// Resource lookup helper. Uses a dedicated ResourceManager + ResourceContext so
/// the language qualifier can be set explicitly — necessary for unpackaged WinUI 3
/// where x:Uid resolution and global language override are flaky.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Manager = new();
    private static readonly ResourceMap Map = Manager.MainResourceMap.TryGetSubtree("Resources")
                                              ?? Manager.MainResourceMap;
    private static ResourceContext _context = BuildContext();

    private static ResourceContext BuildContext()
    {
        var ctx = Manager.CreateResourceContext();
        try
        {
            var pref = new SettingsService().Current.Language;
            var lang = pref switch
            {
                "de" => "de-DE",
                "en" => "en-US",
                _    => string.Empty,
            };
            if (!string.IsNullOrEmpty(lang))
                ctx.QualifierValues["Language"] = lang;
        }
        catch { /* fall back to default */ }
        return ctx;
    }

    /// <summary>Re-evaluate context (e.g. after user changes language at runtime).</summary>
    public static void RefreshContext() => _context = BuildContext();

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        try
        {
            // PRI stores nested keys as path segments ("SettingsTitle/Text"), but
            // we use dot notation in the resw and in code. Translate before lookup.
            var path = key.Replace('.', '/');
            var value = Map.TryGetValue(path, _context);
            return value?.ValueAsString ?? key;
        }
        catch
        {
            return key;
        }
    }

    public static string Format(string key, params object?[] args) =>
        string.Format(Get(key), args);
}
