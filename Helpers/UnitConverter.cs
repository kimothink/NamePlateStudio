namespace NamePlateStudio.Helpers;

/// <summary>
/// WPF uses device independent pixels. At 96 DIP per inch,
/// millimeters must be converted through 25.4 mm per inch for real-size printing.
/// </summary>
public static class UnitConverter
{
    private const double MillimetersPerInch = 25.4;
    private const double WpfPixelsPerInch = 96.0;

    public static double MillimetersToPixels(double millimeters)
    {
        return millimeters / MillimetersPerInch * WpfPixelsPerInch;
    }

    public static double PixelsToMillimeters(double pixels)
    {
        return pixels / WpfPixelsPerInch * MillimetersPerInch;
    }
}
