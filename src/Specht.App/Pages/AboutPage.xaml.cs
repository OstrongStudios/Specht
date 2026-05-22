using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Specht.App.Pages;

public sealed partial class AboutPage : Page
{
    public string VersionText => $"Version {typeof(AboutPage).Assembly.GetName().Version}";

    public AboutPage()
    {
        InitializeComponent();
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}
