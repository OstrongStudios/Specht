namespace Specht.Core.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Update(Action<AppSettings> mutate);
    event EventHandler<AppSettings>? Changed;
}

public sealed class AppSettings
{
    public bool Autostart { get; set; } = false;
    public bool ToastOnNewDevice { get; set; } = true;
    public string Theme { get; set; } = "System"; // System | Dark | Light
    public string Language { get; set; } = "System"; // System | de | en
    public int ScanIntervalSeconds { get; set; } = 0; // 0 = kontinuierlich
    public List<string> HiddenCategories { get; set; } = new();
}
