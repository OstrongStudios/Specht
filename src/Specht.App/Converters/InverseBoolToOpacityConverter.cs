using Microsoft.UI.Xaml.Data;

namespace Specht.App.Converters;

public sealed class InverseBoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (value is bool b && b) ? 0.0 : 1.0;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
