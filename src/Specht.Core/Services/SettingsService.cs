using System.Text.Json;

namespace Specht.Core.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly string _path;
    private readonly object _lock = new();
    private AppSettings _current;

    public SettingsService(string? overridePath = null)
    {
        _path = overridePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Specht", "settings.json");
        _current = Load();
    }

    public AppSettings Current
    {
        get { lock (_lock) return Clone(_current); }
    }

    public event EventHandler<AppSettings>? Changed;

    public void Update(Action<AppSettings> mutate)
    {
        AppSettings copy;
        lock (_lock)
        {
            mutate(_current);
            Save(_current);
            copy = Clone(_current);
        }
        Changed?.Invoke(this, copy);
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded is not null) return loaded;
            }
        }
        catch
        {
            // ignore corrupt settings; fall through to defaults
        }
        return new AppSettings();
    }

    private void Save(AppSettings s)
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path, JsonSerializer.Serialize(s, JsonOpts));
        }
        catch
        {
            // ignore persistence errors; settings still active in-memory
        }
    }

    private static AppSettings Clone(AppSettings s) => new()
    {
        Autostart = s.Autostart,
        ToastOnNewDevice = s.ToastOnNewDevice,
        Theme = s.Theme,
        Language = s.Language,
        ScanIntervalSeconds = s.ScanIntervalSeconds,
        HiddenCategories = new List<string>(s.HiddenCategories ?? new List<string>()),
    };
}
