using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NamePlateStudio.Helpers;

public static class ImageHelper
{
    public static System.Windows.Size? GetInitialImageSizeMm(string imagePath, double maxWidthMm, double maxHeightMm)
    {
        var source = CreateImageSource(imagePath);
        if (source is null || maxWidthMm <= 0 || maxHeightMm <= 0)
        {
            return null;
        }

        var width = UnitConverter.PixelsToMillimeters(source.Width);
        var height = UnitConverter.PixelsToMillimeters(source.Height);
        var scale = Math.Min(1, Math.Min(maxWidthMm / width, maxHeightMm / height));
        return double.IsFinite(scale) && scale > 0
            ? new System.Windows.Size(width * scale, height * scale)
            : null;
    }

    public static ImageSource? CreateImageSource(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
