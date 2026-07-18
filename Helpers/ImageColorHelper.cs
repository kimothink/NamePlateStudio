using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

namespace NamePlateStudio.Helpers;

public static class ImageColorHelper
{
    public static bool TryGetRepresentativeColor(string? imagePath, out string colorHex)
    {
        try
        {
            return TryGetRepresentativeColorCore(imagePath, out colorHex);
        }
        catch
        {
            colorHex = string.Empty;
            return false;
        }
    }

    private static bool TryGetRepresentativeColorCore(string? imagePath, out string colorHex)
    {
        colorHex = string.Empty;
        if (ImageHelper.CreateImageSource(imagePath) is not BitmapSource bitmap ||
            bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            return false;
        }

        var source = bitmap.Format == PixelFormats.Bgra32
            ? bitmap
            : new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        var stride = source.PixelWidth * 4;
        var pixels = new byte[stride * source.PixelHeight];
        source.CopyPixels(pixels, stride, 0);

        var sampleStep = Math.Max(1, Math.Min(source.PixelWidth, source.PixelHeight) / 100);
        var buckets = new Dictionary<int, ColorBucket>();
        long fallbackRed = 0;
        long fallbackGreen = 0;
        long fallbackBlue = 0;
        var fallbackCount = 0;

        for (var y = 0; y < source.PixelHeight; y += sampleStep)
        {
            for (var x = 0; x < source.PixelWidth; x += sampleStep)
            {
                var offset = y * stride + x * 4;
                var blue = pixels[offset];
                var green = pixels[offset + 1];
                var red = pixels[offset + 2];
                var alpha = pixels[offset + 3];
                if (alpha < 128)
                {
                    continue;
                }

                fallbackRed += red;
                fallbackGreen += green;
                fallbackBlue += blue;
                fallbackCount++;

                var maximum = Math.Max(red, Math.Max(green, blue));
                var minimum = Math.Min(red, Math.Min(green, blue));
                var saturation = maximum == 0 ? 0 : (maximum - minimum) / (double)maximum;
                var brightness = maximum / 255.0;
                if (saturation < 0.28 || brightness < 0.32)
                {
                    continue;
                }

                var key = (red >> 4) << 8 | (green >> 4) << 4 | (blue >> 4);
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new ColorBucket();
                    buckets[key] = bucket;
                }

                var weight = saturation * saturation * (0.35 + brightness * brightness);
                bucket.Score += weight;
                bucket.Red += red * weight;
                bucket.Green += green * weight;
                bucket.Blue += blue * weight;
                bucket.Weight += weight;
            }
        }

        byte selectedRed;
        byte selectedGreen;
        byte selectedBlue;
        var selectedBucket = buckets.Count == 0 ? null : buckets.Values.MaxBy(bucket => bucket.Score);
        if (selectedBucket is { Weight: > 0 })
        {
            selectedRed = ToByte(selectedBucket.Red / selectedBucket.Weight);
            selectedGreen = ToByte(selectedBucket.Green / selectedBucket.Weight);
            selectedBlue = ToByte(selectedBucket.Blue / selectedBucket.Weight);
            BrightenFeatureColor(ref selectedRed, ref selectedGreen, ref selectedBlue);
        }
        else if (fallbackCount > 0)
        {
            selectedRed = ToByte(fallbackRed / (double)fallbackCount);
            selectedGreen = ToByte(fallbackGreen / (double)fallbackCount);
            selectedBlue = ToByte(fallbackBlue / (double)fallbackCount);
        }
        else
        {
            return false;
        }

        colorHex = ColorHelper.ToHex(MediaColor.FromRgb(selectedRed, selectedGreen, selectedBlue));
        return true;
    }

    private static void BrightenFeatureColor(ref byte red, ref byte green, ref byte blue)
    {
        var maximum = Math.Max(red, Math.Max(green, blue));
        if (maximum >= 175 || maximum == 0)
        {
            return;
        }

        var scale = 175.0 / maximum;
        red = ToByte(red * scale);
        green = ToByte(green * scale);
        blue = ToByte(blue * scale);
    }

    private static byte ToByte(double value)
        => (byte)Math.Clamp((int)Math.Round(value), byte.MinValue, byte.MaxValue);

    private sealed class ColorBucket
    {
        public double Score { get; set; }
        public double Red { get; set; }
        public double Green { get; set; }
        public double Blue { get; set; }
        public double Weight { get; set; }
    }
}
