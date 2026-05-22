using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Specht.App.ViewModels;
using Specht.Core.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Specht.App.Pages;

public sealed partial class MainPage : Page
{
    private static readonly ExportService Exporter = new();
    public MainViewModel ViewModel { get; } = App.ViewModel;

    public MainPage()
    {
        InitializeComponent();
    }

    private ToggleButton[] AllChips() => new[]
    {
        ChipAll, ChipAirPlay, ChipCast, ChipAudio,
        ChipPrint, ChipHomeKit, ChipFileShare, ChipIoT,
    };

    private void OnChipClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton clicked) return;

        // Single-select semantics: enforce that exactly one chip is checked.
        // If user un-toggled the active one, fall back to "All".
        var chips = AllChips();
        if (clicked.IsChecked != true)
        {
            ChipAll.IsChecked = true;
            foreach (var c in chips) if (c != ChipAll) c.IsChecked = false;
            ViewModel.SetCategoryFilterCommand.Execute("All");
            return;
        }

        foreach (var c in chips) if (c != clicked) c.IsChecked = false;
        var tag = clicked.Tag as string ?? "All";
        ViewModel.SetCategoryFilterCommand.Execute(tag);
    }

    private void OnDeviceClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DeviceViewModel vm)
        {
            Frame.Navigate(typeof(DeviceDetailPage), vm.Device.ServiceInstanceName);
        }
    }

    private async void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        var content = Exporter.ToCsv(App.Cache.Snapshot());
        await SaveAsync(Specht.App.Services.Strings.Get("ExportFileBaseName"), "CSV", ".csv", content);
    }

    private async void OnExportJsonClick(object sender, RoutedEventArgs e)
    {
        var content = Exporter.ToJson(App.Cache.Snapshot());
        await SaveAsync(Specht.App.Services.Strings.Get("ExportFileBaseName"), "JSON", ".json", content);
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) =>
        Frame.Navigate(typeof(SettingsPage));

    private void OnAboutClick(object sender, RoutedEventArgs e) =>
        Frame.Navigate(typeof(AboutPage));

    private void OnExitClick(object sender, RoutedEventArgs e) =>
        ((App)Microsoft.UI.Xaml.Application.Current).RequestExit();

    private async Task SaveAsync(string suggestedName, string label, string extension, string content)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = $"{suggestedName}-{DateTime.Now:yyyyMMdd-HHmm}",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        picker.FileTypeChoices.Add(label, new[] { extension });
        InitializeWithWindow.Initialize(picker, App.MainWindowHwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;
        await File.WriteAllTextAsync(file.Path, content);
    }
}
