using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Specht.App.Converters;

public sealed class IsNewToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isNew && isNew)
        {
            return Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush
                ?? new SolidColorBrush(Colors.DodgerBlue);
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
