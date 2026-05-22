using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Specht.App.ViewModels;

namespace Specht.App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new(App.Settings);

    public string VersionText => $"Specht {ViewModel.AppVersion}";

    public int ScanIndex
    {
        get => ViewModel.ScanIntervalSeconds switch
        {
            0 => 0,
            30 => 1,
            60 => 2,
            300 => 3,
            _ => 0,
        };
        set => ViewModel.ScanIntervalSeconds = value switch
        {
            1 => 30,
            2 => 60,
            3 => 300,
            _ => 0,
        };
    }

    public int LanguageIndex
    {
        get => ViewModel.Language switch
        {
            "de" => 1,
            "en" => 2,
            _    => 0,
        };
        set => ViewModel.Language = value switch
        {
            1 => "de",
            2 => "en",
            _ => "System",
        };
    }

    public int ThemeIndex
    {
        get => ViewModel.Theme switch
        {
            "Dark"  => 1,
            "Light" => 2,
            _       => 0,
        };
        set => ViewModel.Theme = value switch
        {
            1 => "Dark",
            2 => "Light",
            _ => "System",
        };
    }

    public SettingsPage()
    {
        InitializeComponent();
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}
