using System.Globalization;
using System.Windows.Data;

namespace NamePlateStudio.Helpers;

public sealed class MillimetersToPixelsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double millimeters)
        {
            return 0d;
        }

        var allowSignedValue = string.Equals(parameter?.ToString(), "Signed", StringComparison.OrdinalIgnoreCase);
        return UnitConverter.MillimetersToPixels(allowSignedValue ? millimeters : Math.Max(0, millimeters));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}
