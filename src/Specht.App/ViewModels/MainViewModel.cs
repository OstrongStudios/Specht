using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Specht.App.Services;
using Specht.Core;
using Specht.Core.Services;

namespace Specht.App.ViewModels;

public enum DiagnosticState
{
    Discovering,
    HasDevices,
    NoNetwork,
    NoResponses,
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly DeviceCache _cache;
    private readonly DiscoveryService _discovery;
    private readonly DispatcherQueue _dispatcher;
    private readonly DispatcherQueueTimer _searchDebounceTimer;

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();
    public ObservableCollection<DeviceViewModel> FilteredDevices { get; } = new();

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _categoryFilter = "All";

    [ObservableProperty]
    private string _statusText = Strings.Get("StatusStarting");

    [ObservableProperty]
    private DiagnosticState _state = DiagnosticState.Discovering;

    [ObservableProperty]
    private string _emptyHeader = string.Empty;

    [ObservableProperty]
    private string _emptyDetail = string.Empty;

    [ObservableProperty]
    private string _emptyGlyph = string.Empty;

    [ObservableProperty]
    private Visibility _emptyVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _listVisibility = Visibility.Visible;

    public MainViewModel(DeviceCache cache, DiscoveryService discovery)
    {
        _cache = cache;
        _discovery = discovery;
        _dispatcher = DispatcherQueue.GetForCurrentThread()
                      ?? DispatcherQueueController.CreateOnDedicatedThread().DispatcherQueue;
        _searchDebounceTimer = _dispatcher.CreateTimer();
        _searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(150);
        _searchDebounceTimer.IsRepeating = false;
        _searchDebounceTimer.Tick += (_, _) => ApplyFilter();
        _cache.Changed += OnCacheChanged;
        App.Settings.Changed += (_, _) => _dispatcher.TryEnqueue(ApplyFilter);

        _diagnosticTimer = _dispatcher.CreateTimer();
        _diagnosticTimer.Interval = TimeSpan.FromSeconds(1);
        _diagnosticTimer.IsRepeating = true;
        _diagnosticTimer.Tick += (_, _) => UpdateDiagnostic();
        _diagnosticTimer.Start();
    }

    private readonly DispatcherQueueTimer _diagnosticTimer;

    [RelayCommand]
    private void Refresh()
    {
        StatusText = Strings.Get("StatusRefreshing");
        _discovery.Refresh();
    }

    [RelayCommand]
    private void SetCategoryFilter(string category)
    {
        CategoryFilter = category;
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce: restart the timer; ApplyFilter runs once typing pauses.
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void OnCacheChanged(object? sender, DeviceChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(() =>
        {
            switch (e.Kind)
            {
                case DeviceChangeKind.Added:
                    {
                        var vm = new DeviceViewModel(e.Device) { IsNew = true };
                        Devices.Add(vm);
                        ScheduleClearIsNew(vm);
                    }
                    break;
                case DeviceChangeKind.Updated:
                    {
                        var idx = IndexOf(e.Device.ServiceInstanceName);
                        if (idx >= 0)
                        {
                            Devices[idx].UpdateDevice(e.Device);
                            // Trigger UI refresh of computed strings via collection-level change.
                            Devices[idx] = Devices[idx];
                        }
                        else
                        {
                            var vm = new DeviceViewModel(e.Device) { IsNew = true };
                            Devices.Add(vm);
                            ScheduleClearIsNew(vm);
                        }
                    }
                    break;
                case DeviceChangeKind.Removed:
                    {
                        var idx = IndexOf(e.Device.ServiceInstanceName);
                        if (idx >= 0)
                        {
                            var vm = Devices[idx];
                            vm.IsLeaving = true;
                            ScheduleRemoveAfterFade(vm);
                        }
                    }
                    break;
            }
            ApplyFilter();
        });
    }

    private void ScheduleClearIsNew(DeviceViewModel vm)
    {
        var timer = _dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(3);
        timer.IsRepeating = false;
        timer.Tick += (s, _) =>
        {
            vm.IsNew = false;
            (s as DispatcherQueueTimer)?.Stop();
        };
        timer.Start();
    }

    private void ScheduleRemoveAfterFade(DeviceViewModel vm)
    {
        var timer = _dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(700);
        timer.IsRepeating = false;
        timer.Tick += (s, _) =>
        {
            Devices.Remove(vm);
            ApplyFilter();
            (s as DispatcherQueueTimer)?.Stop();
        };
        timer.Start();
    }

    private int IndexOf(string serviceInstanceName)
    {
        for (var i = 0; i < Devices.Count; i++)
            if (string.Equals(Devices[i].Device.ServiceInstanceName, serviceInstanceName, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private void UpdateDiagnostic()
    {
        DiagnosticState next;
        if (Devices.Count > 0)
            next = DiagnosticState.HasDevices;
        else if (!NetworkInterface.GetIsNetworkAvailable())
            next = DiagnosticState.NoNetwork;
        else
        {
            var elapsed = DateTimeOffset.UtcNow - _discovery.StartedAt;
            if (elapsed > TimeSpan.FromSeconds(5) && _discovery.AnswersReceived == 0)
                next = DiagnosticState.NoResponses;
            else
                next = DiagnosticState.Discovering;
        }

        if (next == State) return;
        State = next;
        switch (next)
        {
            case DiagnosticState.HasDevices:
            case DiagnosticState.Discovering:
                EmptyVisibility = Visibility.Collapsed;
                ListVisibility = Visibility.Visible;
                EmptyHeader = string.Empty;
                EmptyDetail = string.Empty;
                EmptyGlyph = string.Empty;
                break;
            case DiagnosticState.NoNetwork:
                EmptyVisibility = Visibility.Visible;
                ListVisibility = Visibility.Collapsed;
                EmptyHeader = Strings.Get("EmptyNoNetworkHeader");
                EmptyDetail = Strings.Get("EmptyNoNetworkDetail");
                EmptyGlyph = ""; // NetworkOffline
                break;
            case DiagnosticState.NoResponses:
                EmptyVisibility = Visibility.Visible;
                ListVisibility = Visibility.Collapsed;
                if (NetworkUtils.IsLikelyVpnActive())
                {
                    EmptyHeader = Strings.Get("EmptyVpnHeader");
                    EmptyDetail = Strings.Get("EmptyVpnDetail");
                }
                else
                {
                    EmptyHeader = Strings.Get("EmptyNoResponsesHeader");
                    EmptyDetail = Strings.Get("EmptyNoResponsesDetail");
                }
                EmptyGlyph = ""; // Info
                break;
        }
    }

    private void ApplyFilter()
    {
        var hidden = App.Settings.Current.HiddenCategories ?? new List<string>();
        FilteredDevices.Clear();
        foreach (var vm in Devices)
        {
            if (!vm.Matches(SearchText)) continue;
            if (CategoryFilter != "All"
                && !string.Equals(vm.CategoryLabel, CategoryFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            if (hidden.Contains(vm.CategoryLabel, StringComparer.OrdinalIgnoreCase))
                continue;
            FilteredDevices.Add(vm);
        }
        StatusText = Strings.Format("StatusFormat", FilteredDevices.Count, Devices.Count, DateTime.Now.ToString("HH:mm:ss"));
    }
}
