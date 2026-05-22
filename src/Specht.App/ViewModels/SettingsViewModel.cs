using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Specht.App.Services;
using Specht.Core.Models;
using Specht.Core.Services;

namespace Specht.App.ViewModels;

public sealed partial class CategoryFilterEntry : ObservableObject
{
    public string Name { get; }
    private readonly Action<string, bool> _onChanged;

    [ObservableProperty]
    private bool _isEnabled;

    public CategoryFilterEntry(string name, bool isEnabled, Action<string, bool> onChanged)
    {
        Name = name;
        _isEnabled = isEnabled;
        _onChanged = onChanged;
    }

    partial void OnIsEnabledChanged(bool value) => _onChanged(Name, value);
}

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    [ObservableProperty]
    private bool _autostart;

    [ObservableProperty]
    private bool _toastOnNewDevice;

    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private string _language = "System";

    [ObservableProperty]
    private int _scanIntervalSeconds;

    public string AppVersion => typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    public ObservableCollection<CategoryFilterEntry> CategoryFilters { get; } = new();

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        var s = _settings.Current;
        _autostart = s.Autostart;
        _toastOnNewDevice = s.ToastOnNewDevice;
        _theme = s.Theme;
        _language = s.Language;
        _scanIntervalSeconds = s.ScanIntervalSeconds;

        var hidden = new HashSet<string>(s.HiddenCategories ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (var cat in Enum.GetNames<ServiceCategory>())
        {
            CategoryFilters.Add(new CategoryFilterEntry(
                cat,
                isEnabled: !hidden.Contains(cat),
                onChanged: OnCategoryToggle));
        }
    }

    private void OnCategoryToggle(string name, bool isEnabled)
    {
        _settings.Update(s =>
        {
            s.HiddenCategories ??= new List<string>();
            if (isEnabled) s.HiddenCategories.RemoveAll(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
            else if (!s.HiddenCategories.Contains(name, StringComparer.OrdinalIgnoreCase)) s.HiddenCategories.Add(name);
        });
    }

    partial void OnAutostartChanged(bool value)
    {
        _settings.Update(s => s.Autostart = value);
        AutostartService.SetEnabled(value);
    }

    partial void OnToastOnNewDeviceChanged(bool value) =>
        _settings.Update(s => s.ToastOnNewDevice = value);

    partial void OnThemeChanged(string value) =>
        _settings.Update(s => s.Theme = value);

    partial void OnLanguageChanged(string value)
    {
        if (_suppressRestart) { _settings.Update(s => s.Language = value); return; }

        _settings.Update(s => s.Language = value);

        var langTag = value switch
        {
            "de" => "de-DE",
            "en" => "en-US",
            _    => string.Empty,
        };
        // Best-effort: just persist the setting and restart. Live override of
        // XAML x:Uid in unpackaged WinUI 3 is unreliable, so we lean on restart.
        try { Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = langTag; }
        catch { /* ignore */ }

        // XAML resources are cached at first parse — restart is required for the
        // change to take full effect across every window/page.
        ((App)Microsoft.UI.Xaml.Application.Current).RestartApp();
    }

    private bool _suppressRestart;

    partial void OnScanIntervalSecondsChanged(int value) =>
        _settings.Update(s => s.ScanIntervalSeconds = value);

    [RelayCommand]
    private void Reset()
    {
        var languageChanged = Language != "System";
        _suppressRestart = true;
        try
        {
            _settings.Update(s =>
            {
                s.Autostart = false;
                s.ToastOnNewDevice = true;
                s.Theme = "System";
                s.Language = "System";
                s.ScanIntervalSeconds = 0;
            });
            AutostartService.SetEnabled(false);
            Autostart = false;
            ToastOnNewDevice = true;
            Theme = "System";
            Language = "System";
            ScanIntervalSeconds = 0;
        }
        finally { _suppressRestart = false; }

        if (languageChanged)
            ((App)Microsoft.UI.Xaml.Application.Current).RestartApp();
    }
}
