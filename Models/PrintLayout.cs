namespace NamePlateStudio.Models;

public sealed class PrintLayout
{
    public double PageWidthMm { get; init; }

    public double PageHeightMm { get; init; }

    public double PageWidthPixels { get; init; }

    public double PageHeightPixels { get; init; }

    public double PlateWidthPixels { get; init; }

    public double PlateHeightPixels { get; init; }

    public double MarginPixels { get; init; }

    public double PrintableWidthPixels { get; init; }

    public double PrintableHeightPixels { get; init; }

    public int Columns { get; init; }

    public int Rows { get; init; }

    public int ItemsPerPage { get; init; }

    public int PageCount { get; init; }

    public IReadOnlyList<NamePlatePlacement> Placements { get; init; } = [];

    public bool CanFit => ItemsPerPage > 0;

    public IEnumerable<int> PageIndexes => Enumerable.Range(0, Math.Max(0, PageCount));
}
