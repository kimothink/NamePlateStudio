using System.Globalization;
using System.Windows.Data;

namespace NamePlateStudio.Helpers;

public sealed class AlignmentOffsetConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double size)
        {
            return 0.0;
        }

        var alignment = values[1]?.ToString() ?? string.Empty;

        return alignment switch
        {
            "왼쪽" or "위" or "Left" or "Top" => 0.0,
            "오른쪽" or "아래" or "Right" or "Bottom" => -size,
            _ => -size / 2.0
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
