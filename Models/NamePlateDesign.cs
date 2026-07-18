namespace NamePlateStudio.Models;

/// <summary>
/// JSON save/load target for one name plate design and its paper layout settings.
/// PositionX and PositionY are stored in millimeters and point to the name text anchor.
/// </summary>
public sealed class NamePlateDesign
{
    public double WidthMm { get; set; } = 200;

    public double HeightMm { get; set; } = 80;

    public string NameText { get; set; } = "홍길동";

    public string TitleText { get; set; } = "개발자";

    public string CompanyText { get; set; } = "NamePlateStudio";

    public List<NamePlateEntry> Entries { get; set; } =
    [
        new("고요한", "개발자", "NamePlateStudio"),
        new("신일수", "기획자", "NamePlateStudio"),
        new("이청룡", "디자이너", "NamePlateStudio")
    ];

    public string FontFamily { get; set; } = "Malgun Gothic";

    public double FontSize { get; set; } = 42;

    public string FontColor { get; set; } = "#111827";

    public string BackgroundColor { get; set; } = "#FFFFFF";

    public bool MatchFrontBackgroundToImage { get; set; }

    public string BorderColor { get; set; } = "#1F2937";

    public string BackFontColor { get; set; } = "#111827";

    public string BackBackgroundColor { get; set; } = "#FFFFFF";

    public string BackBorderColor { get; set; } = "#1F2937";

    public bool MatchBackBackgroundToImage { get; set; }

    public double BorderThickness { get; set; } = 2;

    public double PositionX { get; set; } = 100;

    public double PositionY { get; set; } = 40;

    public bool IsBold { get; set; } = true;

    public bool IsItalic { get; set; }

    public string HorizontalTextAlignment { get; set; } = "가운데";

    public string VerticalTextAlignment { get; set; } = "가운데";

    public bool HasDecorationLine { get; set; } = true;

    public double OverlayImageX { get; set; } = 8;

    public double OverlayImageY { get; set; } = 8;

    public double OverlayImageWidth { get; set; } = 18;

    public double OverlayImageHeight { get; set; } = 18;

    public double OverlayImageRotation { get; set; }

    public bool ApplyFrontImagesToAllEntries { get; set; }

    public bool ApplyBackImagesToAllEntries { get; set; }

    public bool LockImageAspectRatio { get; set; } = true;

    public string PaperSizeName { get; set; } = "A4";

    public double PaperWidthMm { get; set; } = 210;

    public double PaperHeightMm { get; set; } = 297;

    public bool IsLandscape { get; set; }

    public bool IsDoubleSided { get; set; }

    public double BackFontSize { get; set; } = 20;

    public int CopyCount { get; set; } = 3;

    public double PageMarginMm { get; set; } = 5;

    public double HorizontalGapMm { get; set; } = 5;

    public double VerticalGapMm { get; set; } = 5;
}
