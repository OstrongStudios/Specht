using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Specht.App.ViewModels;

namespace Specht.App.Pages;

public sealed partial class DeviceDetailPage : Page
{
    public DeviceDetailViewModel ViewModel { get; private set; } = null!;

    public DeviceDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var serviceInstanceName = e.Parameter as string ?? "";
        ViewModel = new DeviceDetailViewModel(App.Cache, serviceInstanceName);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel?.Dispose();
        base.OnNavigatedFrom(e);
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}
