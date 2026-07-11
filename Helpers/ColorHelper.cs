using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Colors = System.Windows.Media.Colors;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace NamePlateStudio.Helpers;

public static class ColorHelper
{
    public static bool TryParseColor(string? colorText, out Color color)
    {
        try
        {
            var converted = ColorConverter.ConvertFromString(colorText ?? string.Empty);
            if (converted is Color parsedColor)
            {
                color = parsedColor;
                return true;
            }
        }
        catch
        {
            // Invalid color text is handled by the caller.
        }

        color = Colors.Transparent;
        return false;
    }

    public static bool TryCreateBrush(string? colorText, out Brush brush)
    {
        if (TryParseColor(colorText, out var color))
        {
            var parsedBrush = new SolidColorBrush(color);
            if (parsedBrush.CanFreeze)
            {
                parsedBrush.Freeze();
            }

            brush = parsedBrush;
            return true;
        }

        brush = Brushes.Transparent;
        return false;
    }

    public static Brush ToBrush(string? colorText, Brush fallback)
    {
        return TryCreateBrush(colorText, out var brush) ? brush : fallback;
    }

    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
