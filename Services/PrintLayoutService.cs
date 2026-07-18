using NamePlateStudio.Helpers;
using NamePlateStudio.Models;

namespace NamePlateStudio.Services;

public sealed class PrintLayoutService
{
    public PrintLayout CreateLayout(NamePlateDesign design)
    {
        var pageWidthMm = design.IsLandscape ? design.PaperHeightMm : design.PaperWidthMm;
        var pageHeightMm = design.IsLandscape ? design.PaperWidthMm : design.PaperHeightMm;
        var pageWidthPixels = UnitConverter.MillimetersToPixels(Math.Max(1, pageWidthMm));
        var pageHeightPixels = UnitConverter.MillimetersToPixels(Math.Max(1, pageHeightMm));
        var plateWidthPixels = UnitConverter.MillimetersToPixels(Math.Max(1, design.WidthMm));
        var plateHeightPixels = UnitConverter.MillimetersToPixels(Math.Max(1, design.HeightMm));
        var marginMm = Math.Max(0, design.PageMarginMm);
        var horizontalGapMm = Math.Max(0, design.HorizontalGapMm);
        var verticalGapMm = Math.Max(0, design.VerticalGapMm);
        var entries = GetPrintableEntries(design);
        var copyCount = entries.Count;

        var printableWidthMm = pageWidthMm - marginMm * 2;
        var printableHeightMm = pageHeightMm - marginMm * 2;

        var columns = GetFitCount(printableWidthMm, design.WidthMm, horizontalGapMm);
        var rows = GetFitCount(printableHeightMm, design.HeightMm, verticalGapMm);
        var itemsPerPage = columns * rows;

        if (itemsPerPage <= 0)
        {
            return new PrintLayout
            {
                PageWidthMm = pageWidthMm,
                PageHeightMm = pageHeightMm,
                PageWidthPixels = pageWidthPixels,
                PageHeightPixels = pageHeightPixels,
                PlateWidthPixels = plateWidthPixels,
                PlateHeightPixels = plateHeightPixels,
                MarginPixels = UnitConverter.MillimetersToPixels(marginMm),
                PrintableWidthPixels = UnitConverter.MillimetersToPixels(Math.Max(0, printableWidthMm)),
                PrintableHeightPixels = UnitConverter.MillimetersToPixels(Math.Max(0, printableHeightMm))
            };
        }

        var pageCount = (int)Math.Ceiling(copyCount / (double)itemsPerPage);
        var placements = new List<NamePlatePlacement>(copyCount);
        var usedWidthMm = columns * design.WidthMm + Math.Max(0, columns - 1) * horizontalGapMm;
        var usedHeightMm = rows * design.HeightMm + Math.Max(0, rows - 1) * verticalGapMm;
        var startXmm = (pageWidthMm - usedWidthMm) / 2.0;
        var startYmm = (pageHeightMm - usedHeightMm) / 2.0;

        for (var index = 0; index < copyCount; index++)
        {
            var pageIndex = index / itemsPerPage;
            var indexOnPage = index % itemsPerPage;
            var row = indexOnPage / columns;
            var column = indexOnPage % columns;
            var leftMm = startXmm + column * (design.WidthMm + horizontalGapMm);
            var topMm = startYmm + row * (design.HeightMm + verticalGapMm);

            placements.Add(new NamePlatePlacement
            {
                CopyNumber = index + 1,
                Entry = entries[index],
                PageIndex = pageIndex,
                Row = row,
                Column = column,
                LeftPixels = UnitConverter.MillimetersToPixels(leftMm),
                TopPixels = UnitConverter.MillimetersToPixels(topMm),
                WidthPixels = plateWidthPixels,
                HeightPixels = plateHeightPixels
            });
        }

        return new PrintLayout
        {
            PageWidthMm = pageWidthMm,
            PageHeightMm = pageHeightMm,
            PageWidthPixels = pageWidthPixels,
            PageHeightPixels = pageHeightPixels,
            PlateWidthPixels = plateWidthPixels,
            PlateHeightPixels = plateHeightPixels,
            MarginPixels = UnitConverter.MillimetersToPixels(marginMm),
            PrintableWidthPixels = UnitConverter.MillimetersToPixels(Math.Max(0, printableWidthMm)),
            PrintableHeightPixels = UnitConverter.MillimetersToPixels(Math.Max(0, printableHeightMm)),
            Columns = columns,
            Rows = rows,
            ItemsPerPage = itemsPerPage,
            PageCount = pageCount,
            Placements = placements
        };
    }

    private static int GetFitCount(double availableMm, double itemMm, double gapMm)
    {
        if (availableMm <= 0 || itemMm <= 0)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Floor((availableMm + gapMm) / (itemMm + gapMm)));
    }

    private static List<NamePlateEntry> GetPrintableEntries(NamePlateDesign design)
    {
        var entries = (design.Entries ?? [])
            .Where(entry => !string.IsNullOrWhiteSpace(entry.NameText))
            .Select(entry => new NamePlateEntry(
                (entry.NameText ?? string.Empty).Trim(),
                (entry.TitleText ?? string.Empty).Trim(),
                (entry.CompanyText ?? string.Empty).Trim(),
                entry.BackgroundImagePath ?? string.Empty,
                entry.OverlayImagePath ?? string.Empty,
                entry.BackContent ?? string.Empty,
                entry.BackImagePath ?? string.Empty,
                entry.BackImageWidthMm,
                entry.BackImageHeightMm,
                entry.BackTableRows,
                entry.BackTableColumns,
                entry.BackTableCells,
                entry.BackImageX,
                entry.BackImageY,
                entry.BackImageRotation))
            .ToList();

        if (entries.Count > 0)
        {
            return entries;
        }

        if (string.IsNullOrWhiteSpace(design.NameText))
        {
            return [];
        }

        return
        [
            new NamePlateEntry(
                (design.NameText ?? string.Empty).Trim(),
                (design.TitleText ?? string.Empty).Trim(),
                (design.CompanyText ?? string.Empty).Trim())
        ];
    }
}
