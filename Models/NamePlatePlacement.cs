namespace NamePlateStudio.Models;

public sealed class NamePlatePlacement
{
    public int CopyNumber { get; init; }

    public NamePlateEntry Entry { get; init; } = new();

    public int PageIndex { get; init; }

    public int Row { get; init; }

    public int Column { get; init; }

    public double LeftPixels { get; init; }

    public double TopPixels { get; init; }

    public double WidthPixels { get; init; }

    public double HeightPixels { get; init; }
}
