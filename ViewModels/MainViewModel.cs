using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NamePlateStudio.Helpers;
using NamePlateStudio.Models;
using NamePlateStudio.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontStyle = System.Windows.FontStyle;
using FontStyles = System.Windows.FontStyles;
using MessageBox = System.Windows.MessageBox;

namespace NamePlateStudio.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly FileService fileService = new();
    private readonly PrintService printService = new();
    private readonly ColorDialogService colorDialogService = new();
    private readonly ImageFileService imageFileService = new();
    private readonly PrintLayoutService layoutService = new();

    public MainViewModel()
    {
        FontFamilies = new ObservableCollection<string>(
            Fonts.SystemFontFamilies
                .Select(font => font.Source)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(font => font));

        if (!FontFamilies.Contains(SelectedFontFamily))
        {
            FontFamilies.Insert(0, SelectedFontFamily);
        }

        PaperSizes =
        [
            new PaperSizeOption("A4", 210, 297),
            new PaperSizeOption("A3", 297, 420),
            new PaperSizeOption("Letter", 215.9, 279.4),
            new PaperSizeOption("사용자 지정", 210, 297, IsCustom: true)
        ];
        selectedPaperSize = PaperSizes[0];

        Entries.CollectionChanged += EntriesCollectionChanged;
        ApplyEntries(new NamePlateDesign().Entries);
        RefreshPreviewProperties();
    }

    public ObservableCollection<string> FontFamilies { get; }

    public ObservableCollection<PaperSizeOption> PaperSizes { get; }

    public ObservableCollection<NamePlateEntry> Entries { get; } = [];

    public ObservableCollection<NamePlatePlacement> PreviewPlacements { get; } = [];

    public IReadOnlyList<string> HorizontalAlignmentOptions { get; } = ["왼쪽", "가운데", "오른쪽"];

    public IReadOnlyList<string> VerticalAlignmentOptions { get; } = ["위", "가운데", "아래"];

    [ObservableProperty]
    private double widthMm = 200;

    [ObservableProperty]
    private double heightMm = 80;

    [ObservableProperty]
    private string selectedFontFamily = "Malgun Gothic";

    [ObservableProperty]
    private double fontSize = 42;

    [ObservableProperty]
    private string fontColor = "#111827";

    [ObservableProperty]
    private string backgroundColor = "#FFFFFF";

    [ObservableProperty]
    private string borderColor = "#1F2937";

    [ObservableProperty]
    private double borderThickness = 2;

    [ObservableProperty]
    private double positionX = 100;

    [ObservableProperty]
    private double positionY = 40;

    [ObservableProperty]
    private bool isBold = true;

    [ObservableProperty]
    private bool isItalic;

    [ObservableProperty]
    private string horizontalTextAlignment = "가운데";

    [ObservableProperty]
    private string verticalTextAlignment = "가운데";

    [ObservableProperty]
    private bool hasDecorationLine = true;

    [ObservableProperty]
    private double overlayImageX = 8;

    [ObservableProperty]
    private double overlayImageY = 8;

    [ObservableProperty]
    private double overlayImageWidth = 18;

    [ObservableProperty]
    private double overlayImageHeight = 18;

    [ObservableProperty]
    private PaperSizeOption? selectedPaperSize;

    [ObservableProperty]
    private double paperWidthMm = 210;

    [ObservableProperty]
    private double paperHeightMm = 297;

    [ObservableProperty]
    private bool isLandscape;

    [ObservableProperty]
    private double pageMarginMm = 5;

    [ObservableProperty]
    private double horizontalGapMm = 5;

    [ObservableProperty]
    private double verticalGapMm = 5;

    [ObservableProperty]
    private double zoom = 0.7;

    [ObservableProperty]
    private NamePlateEntry? selectedEntry;

    [ObservableProperty]
    private string layoutSummaryText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "명패 내용 목록을 입력하면 각 명패에 서로 다른 표시명/소개 문구/소속 또는 메모가 표시됩니다.";

    public double PreviewWidthPixels => UnitConverter.MillimetersToPixels(Math.Max(1, WidthMm));

    public double PreviewHeightPixels => UnitConverter.MillimetersToPixels(Math.Max(1, HeightMm));

    public double EffectivePaperWidthMm => IsLandscape ? PaperHeightMm : PaperWidthMm;

    public double EffectivePaperHeightMm => IsLandscape ? PaperWidthMm : PaperHeightMm;

    public double EffectivePaperWidthPixels => UnitConverter.MillimetersToPixels(Math.Max(1, EffectivePaperWidthMm));

    public double EffectivePaperHeightPixels => UnitConverter.MillimetersToPixels(Math.Max(1, EffectivePaperHeightMm));

    public double PageMarginPixels => UnitConverter.MillimetersToPixels(Math.Max(0, PageMarginMm));

    public double PrintableAreaWidthPixels => UnitConverter.MillimetersToPixels(Math.Max(0, EffectivePaperWidthMm - PageMarginMm * 2));

    public double PrintableAreaHeightPixels => UnitConverter.MillimetersToPixels(Math.Max(0, EffectivePaperHeightMm - PageMarginMm * 2));

    public double PositionXPixels => UnitConverter.MillimetersToPixels(PositionX);

    public double PositionYPixels => UnitConverter.MillimetersToPixels(PositionY);

    public double TitleFontSize => Math.Max(10, FontSize * 0.42);

    public double CompanyFontSize => Math.Max(10, FontSize * 0.36);

    public double TitlePositionYPixels => PositionYPixels + FontSize * 0.82;

    public double CompanyPositionYPixels => PositionYPixels - FontSize * 0.95;

    public double DecorationLineStartX => PreviewWidthPixels * 0.08;

    public double DecorationLineEndX => PreviewWidthPixels * 0.92;

    public double DecorationTopLineY => PreviewHeightPixels * 0.24;

    public double DecorationBottomLineY => PreviewHeightPixels * 0.76;

    public double OverlayImageXPixels => UnitConverter.MillimetersToPixels(Math.Max(0, OverlayImageX));

    public double OverlayImageYPixels => UnitConverter.MillimetersToPixels(Math.Max(0, OverlayImageY));

    public double OverlayImageWidthPixels => UnitConverter.MillimetersToPixels(Math.Max(0, OverlayImageWidth));

    public double OverlayImageHeightPixels => UnitConverter.MillimetersToPixels(Math.Max(0, OverlayImageHeight));

    public string PlateSizeText => $"{WidthMm:0.##} mm x {HeightMm:0.##} mm";

    public string PaperSizeText => $"{EffectivePaperWidthMm:0.##} mm x {EffectivePaperHeightMm:0.##} mm";

    public string EntryCountText => $"{GetPrintableEntries(trimText: false).Count}명";

    public string ZoomPercentText => $"{Zoom * 100:0}%";

    public Brush FontBrush => ColorHelper.ToBrush(FontColor, Brushes.Black);

    public Brush BackgroundBrush => ColorHelper.ToBrush(BackgroundColor, Brushes.White);

    public Brush BorderBrush => ColorHelper.ToBrush(BorderColor, Brushes.Black);

    public Thickness PreviewBorderThickness => new(Math.Max(0, BorderThickness));

    public FontWeight NameFontWeight => IsBold ? FontWeights.Bold : FontWeights.Normal;

    public FontStyle NameFontStyle => IsItalic ? FontStyles.Italic : FontStyles.Normal;

    [RelayCommand]
    private void AddEntry()
    {
        var entry = new NamePlateEntry("새 항목", string.Empty, string.Empty);
        Entries.Add(entry);
        SelectedEntry = entry;
        StatusMessage = "명패 항목을 추가했습니다.";
    }

    [RelayCommand]
    private void RemoveEntry()
    {
        if (SelectedEntry is null)
        {
            ShowNotice("삭제할 항목 행을 선택해주세요.");
            return;
        }

        Entries.Remove(SelectedEntry);
        SelectedEntry = Entries.FirstOrDefault();
        StatusMessage = "선택한 항목을 삭제했습니다.";
    }

    [RelayCommand]
    private void SelectEntryBackgroundImage()
    {
        SetSelectedEntryImage(isBackground: true);
    }

    [RelayCommand]
    private void ClearEntryBackgroundImage()
    {
        if (SelectedEntry is null)
        {
            ShowNotice("이미지를 지울 항목을 선택해주세요.");
            return;
        }

        SelectedEntry.BackgroundImagePath = string.Empty;
        StatusMessage = "선택한 항목의 배경 이미지를 제거했습니다.";
    }

    [RelayCommand]
    private void SelectEntryOverlayImage()
    {
        SetSelectedEntryImage(isBackground: false);
    }

    [RelayCommand]
    private void ClearEntryOverlayImage()
    {
        if (SelectedEntry is null)
        {
            ShowNotice("이미지를 지울 항목을 선택해주세요.");
            return;
        }

        SelectedEntry.OverlayImagePath = string.Empty;
        StatusMessage = "선택한 항목의 이미지/로고를 제거했습니다.";
    }

    [RelayCommand]
    private void Save()
    {
        if (!TryCreateDesign(out var design))
        {
            return;
        }

        try
        {
            var path = fileService.Save(design!);
            StatusMessage = path is null ? "저장이 취소되었습니다." : $"저장 완료: {path}";
        }
        catch (Exception ex)
        {
            ShowError($"JSON 저장에 실패했습니다.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            var (design, path) = fileService.Load();
            if (design is null)
            {
                StatusMessage = "불러오기가 취소되었습니다.";
                return;
            }

            ApplyDesign(design);
            StatusMessage = $"불러오기 완료: {path}";
        }
        catch (Exception ex)
        {
            ShowError($"JSON 불러오기에 실패했습니다.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void Print()
    {
        if (!TryCreateDesign(out var design))
        {
            return;
        }

        if (printService.Print(design!, out var message))
        {
            StatusMessage = message;
        }
        else
        {
            ShowNotice(message);
        }
    }

    [RelayCommand]
    private void Preview()
    {
        if (!TryCreateDesign(out var design))
        {
            return;
        }

        printService.ShowPrintPreview(design!);
        StatusMessage = "항목별 명패 인쇄 미리보기를 확인했습니다.";
    }

    [RelayCommand]
    private void Reset()
    {
        ApplyDesign(new NamePlateDesign());
        Zoom = 0.7;
        StatusMessage = "기본 명패 내용 목록과 A4 다중 출력 설정으로 초기화했습니다.";
    }

    [RelayCommand]
    private void CenterText()
    {
        CenterTextPosition();
        StatusMessage = "대표 문구 위치를 현재 명패 크기의 중앙으로 맞췄습니다.";
    }

    [RelayCommand]
    private void SelectFontColor()
    {
        PickColor("글자", FontColor, selectedColor => FontColor = selectedColor);
    }

    [RelayCommand]
    private void SelectBackgroundColor()
    {
        PickColor("배경", BackgroundColor, selectedColor => BackgroundColor = selectedColor);
    }

    [RelayCommand]
    private void SelectBorderColor()
    {
        PickColor("테두리", BorderColor, selectedColor => BorderColor = selectedColor);
    }

    partial void OnWidthMmChanged(double value)
    {
        PositionX = GetCenterPosition(value);
        RefreshPreviewProperties();
    }

    partial void OnHeightMmChanged(double value)
    {
        PositionY = GetCenterPosition(value);
        RefreshPreviewProperties();
    }

    partial void OnSelectedFontFamilyChanged(string value) => RefreshPreviewProperties();

    partial void OnFontSizeChanged(double value) => RefreshPreviewProperties();

    partial void OnFontColorChanged(string value) => RefreshPreviewProperties();

    partial void OnBackgroundColorChanged(string value) => RefreshPreviewProperties();

    partial void OnBorderColorChanged(string value) => RefreshPreviewProperties();

    partial void OnBorderThicknessChanged(double value) => RefreshPreviewProperties();

    partial void OnPositionXChanged(double value) => RefreshPreviewProperties();

    partial void OnPositionYChanged(double value) => RefreshPreviewProperties();

    partial void OnIsBoldChanged(bool value) => RefreshPreviewProperties();

    partial void OnIsItalicChanged(bool value) => RefreshPreviewProperties();

    partial void OnHorizontalTextAlignmentChanged(string value) => RefreshPreviewProperties();

    partial void OnVerticalTextAlignmentChanged(string value) => RefreshPreviewProperties();

    partial void OnHasDecorationLineChanged(bool value) => RefreshPreviewProperties();

    partial void OnOverlayImageXChanged(double value) => RefreshPreviewProperties();

    partial void OnOverlayImageYChanged(double value) => RefreshPreviewProperties();

    partial void OnOverlayImageWidthChanged(double value) => RefreshPreviewProperties();

    partial void OnOverlayImageHeightChanged(double value) => RefreshPreviewProperties();

    partial void OnSelectedPaperSizeChanged(PaperSizeOption? value)
    {
        if (value is { IsCustom: false })
        {
            PaperWidthMm = value.WidthMm;
            PaperHeightMm = value.HeightMm;
        }

        RefreshPreviewProperties();
    }

    partial void OnPaperWidthMmChanged(double value) => RefreshPreviewProperties();

    partial void OnPaperHeightMmChanged(double value) => RefreshPreviewProperties();

    partial void OnIsLandscapeChanged(bool value) => RefreshPreviewProperties();

    partial void OnPageMarginMmChanged(double value) => RefreshPreviewProperties();

    partial void OnHorizontalGapMmChanged(double value) => RefreshPreviewProperties();

    partial void OnVerticalGapMmChanged(double value) => RefreshPreviewProperties();

    partial void OnZoomChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomPercentText));
    }

    private bool TryCreateDesign(out NamePlateDesign? design)
    {
        design = null;

        if (WidthMm <= 0 || HeightMm <= 0)
        {
            ShowNotice("명패 가로/세로 값은 0보다 커야 합니다.");
            return false;
        }

        if (FontSize <= 0)
        {
            ShowNotice("글자 크기는 0보다 커야 합니다.");
            return false;
        }

        if (BorderThickness < 0)
        {
            ShowNotice("테두리 두께는 0 이상이어야 합니다.");
            return false;
        }

        if (PaperWidthMm <= 0 || PaperHeightMm <= 0)
        {
            ShowNotice("용지 가로/세로 값은 0보다 커야 합니다.");
            return false;
        }

        var entries = GetPrintableEntries(trimText: true);
        if (entries.Count == 0)
        {
            ShowNotice("명패 내용 목록에 표시명을 1개 이상 입력해주세요.");
            return false;
        }

        if (entries.Count > 999)
        {
            ShowNotice("명패 항목은 한 번에 999개 이하로 입력해주세요.");
            return false;
        }

        if (PageMarginMm < 0 || HorizontalGapMm < 0 || VerticalGapMm < 0)
        {
            ShowNotice("여백과 간격은 0 이상이어야 합니다.");
            return false;
        }

        if (OverlayImageX < 0 || OverlayImageY < 0 || OverlayImageWidth < 0 || OverlayImageHeight < 0)
        {
            ShowNotice("이미지 위치와 크기는 0 이상이어야 합니다.");
            return false;
        }

        if (!ColorHelper.TryCreateBrush(FontColor, out _))
        {
            ShowNotice("글자 색상을 다시 선택해주세요.");
            return false;
        }

        if (!ColorHelper.TryCreateBrush(BackgroundColor, out _))
        {
            ShowNotice("배경색을 다시 선택해주세요.");
            return false;
        }

        if (!ColorHelper.TryCreateBrush(BorderColor, out _))
        {
            ShowNotice("테두리 색상을 다시 선택해주세요.");
            return false;
        }

        design = CreateDesignSnapshot(entries);
        var layout = layoutService.CreateLayout(design);
        if (!layout.CanFit)
        {
            ShowNotice("현재 용지 크기, 여백, 간격 안에 명패가 들어가지 않습니다.");
            return false;
        }

        return true;
    }

    private NamePlateDesign CreateDesignSnapshot(List<NamePlateEntry>? entries = null)
    {
        entries ??= GetPrintableEntries(trimText: false);
        var firstEntry = entries.FirstOrDefault() ?? new NamePlateEntry();

        return new NamePlateDesign
        {
            WidthMm = WidthMm,
            HeightMm = HeightMm,
            NameText = firstEntry.NameText,
            TitleText = firstEntry.TitleText,
            CompanyText = firstEntry.CompanyText,
            Entries = entries,
            FontFamily = SelectedFontFamily,
            FontSize = FontSize,
            FontColor = FontColor,
            BackgroundColor = BackgroundColor,
            BorderColor = BorderColor,
            BorderThickness = BorderThickness,
            PositionX = PositionX,
            PositionY = PositionY,
            IsBold = IsBold,
            IsItalic = IsItalic,
            HorizontalTextAlignment = HorizontalTextAlignment,
            VerticalTextAlignment = VerticalTextAlignment,
            HasDecorationLine = HasDecorationLine,
            OverlayImageX = OverlayImageX,
            OverlayImageY = OverlayImageY,
            OverlayImageWidth = OverlayImageWidth,
            OverlayImageHeight = OverlayImageHeight,
            PaperSizeName = SelectedPaperSize?.Name ?? "사용자 지정",
            PaperWidthMm = PaperWidthMm,
            PaperHeightMm = PaperHeightMm,
            IsLandscape = IsLandscape,
            CopyCount = Math.Max(1, entries.Count),
            PageMarginMm = PageMarginMm,
            HorizontalGapMm = HorizontalGapMm,
            VerticalGapMm = VerticalGapMm
        };
    }

    private void ApplyDesign(NamePlateDesign design)
    {
        WidthMm = design.WidthMm;
        HeightMm = design.HeightMm;
        SelectedFontFamily = string.IsNullOrWhiteSpace(design.FontFamily) ? "Malgun Gothic" : design.FontFamily;
        FontSize = design.FontSize <= 0 ? 42 : design.FontSize;
        FontColor = string.IsNullOrWhiteSpace(design.FontColor) ? "#111827" : design.FontColor;
        BackgroundColor = string.IsNullOrWhiteSpace(design.BackgroundColor) ? "#FFFFFF" : design.BackgroundColor;
        BorderColor = string.IsNullOrWhiteSpace(design.BorderColor) ? "#1F2937" : design.BorderColor;
        BorderThickness = Math.Max(0, design.BorderThickness);
        PositionX = design.PositionX;
        PositionY = design.PositionY;
        IsBold = design.IsBold;
        IsItalic = design.IsItalic;
        HorizontalTextAlignment = NormalizeHorizontalAlignment(design.HorizontalTextAlignment);
        VerticalTextAlignment = NormalizeVerticalAlignment(design.VerticalTextAlignment);
        HasDecorationLine = design.HasDecorationLine;
        OverlayImageX = Math.Max(0, design.OverlayImageX);
        OverlayImageY = Math.Max(0, design.OverlayImageY);
        OverlayImageWidth = design.OverlayImageWidth <= 0 ? 18 : design.OverlayImageWidth;
        OverlayImageHeight = design.OverlayImageHeight <= 0 ? 18 : design.OverlayImageHeight;

        var entries = design.Entries is { Count: > 0 }
            ? design.Entries
            : [new NamePlateEntry(design.NameText, design.TitleText, design.CompanyText)];
        ApplyEntries(entries);

        var paperOption = PaperSizes.FirstOrDefault(option => option.Name == design.PaperSizeName)
            ?? PaperSizes.First(option => option.IsCustom);
        SelectedPaperSize = paperOption;
        PaperWidthMm = design.PaperWidthMm <= 0 ? paperOption.WidthMm : design.PaperWidthMm;
        PaperHeightMm = design.PaperHeightMm <= 0 ? paperOption.HeightMm : design.PaperHeightMm;
        IsLandscape = design.IsLandscape;
        PageMarginMm = Math.Max(0, design.PageMarginMm);
        HorizontalGapMm = Math.Max(0, design.HorizontalGapMm);
        VerticalGapMm = Math.Max(0, design.VerticalGapMm);

        RefreshPreviewProperties();
    }

    private void ApplyEntries(IEnumerable<NamePlateEntry> entries)
    {
        foreach (var entry in Entries)
        {
            entry.PropertyChanged -= EntryPropertyChanged;
        }

        Entries.Clear();
        foreach (var entry in entries)
        {
            var cleanEntry = new NamePlateEntry(
                entry.NameText ?? string.Empty,
                entry.TitleText ?? string.Empty,
                entry.CompanyText ?? string.Empty,
                entry.BackgroundImagePath ?? string.Empty,
                entry.OverlayImagePath ?? string.Empty);
            Entries.Add(cleanEntry);
        }

        SelectedEntry = Entries.FirstOrDefault();
    }

    private void EntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (NamePlateEntry entry in e.OldItems)
            {
                entry.PropertyChanged -= EntryPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (NamePlateEntry entry in e.NewItems)
            {
                entry.PropertyChanged += EntryPropertyChanged;
            }
        }

        RefreshPreviewProperties();
    }

    private void EntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshPreviewProperties();
    }

    private void RefreshPreviewProperties()
    {
        OnPropertyChanged(nameof(PreviewWidthPixels));
        OnPropertyChanged(nameof(PreviewHeightPixels));
        OnPropertyChanged(nameof(EffectivePaperWidthMm));
        OnPropertyChanged(nameof(EffectivePaperHeightMm));
        OnPropertyChanged(nameof(EffectivePaperWidthPixels));
        OnPropertyChanged(nameof(EffectivePaperHeightPixels));
        OnPropertyChanged(nameof(PageMarginPixels));
        OnPropertyChanged(nameof(PrintableAreaWidthPixels));
        OnPropertyChanged(nameof(PrintableAreaHeightPixels));
        OnPropertyChanged(nameof(PositionXPixels));
        OnPropertyChanged(nameof(PositionYPixels));
        OnPropertyChanged(nameof(TitleFontSize));
        OnPropertyChanged(nameof(CompanyFontSize));
        OnPropertyChanged(nameof(TitlePositionYPixels));
        OnPropertyChanged(nameof(CompanyPositionYPixels));
        OnPropertyChanged(nameof(DecorationLineStartX));
        OnPropertyChanged(nameof(DecorationLineEndX));
        OnPropertyChanged(nameof(DecorationTopLineY));
        OnPropertyChanged(nameof(DecorationBottomLineY));
        OnPropertyChanged(nameof(OverlayImageXPixels));
        OnPropertyChanged(nameof(OverlayImageYPixels));
        OnPropertyChanged(nameof(OverlayImageWidthPixels));
        OnPropertyChanged(nameof(OverlayImageHeightPixels));
        OnPropertyChanged(nameof(PlateSizeText));
        OnPropertyChanged(nameof(PaperSizeText));
        OnPropertyChanged(nameof(EntryCountText));
        OnPropertyChanged(nameof(FontBrush));
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(PreviewBorderThickness));
        OnPropertyChanged(nameof(NameFontWeight));
        OnPropertyChanged(nameof(NameFontStyle));
        RebuildPreviewPlacements();
    }

    private void RebuildPreviewPlacements()
    {
        PreviewPlacements.Clear();

        var entryCount = GetPrintableEntries(trimText: false).Count;
        if (entryCount == 0)
        {
            LayoutSummaryText = "명패 내용 목록에 표시명을 입력해주세요.";
            return;
        }

        var layout = layoutService.CreateLayout(CreateDesignSnapshot());
        foreach (var placement in layout.Placements.Where(item => item.PageIndex == 0))
        {
            PreviewPlacements.Add(placement);
        }

        LayoutSummaryText = layout.CanFit
            ? $"항목 {entryCount}개, 한 페이지 {layout.Columns}열 x {layout.Rows}행 = {layout.ItemsPerPage}개, 총 {layout.PageCount}페이지"
            : "현재 설정으로는 용지 안에 명패가 들어가지 않습니다.";
    }

    private List<NamePlateEntry> GetPrintableEntries(bool trimText)
    {
        return Entries
            .Select(entry =>
            {
                var name = entry.NameText ?? string.Empty;
                var title = entry.TitleText ?? string.Empty;
                var company = entry.CompanyText ?? string.Empty;

                return new NamePlateEntry(
                    trimText ? name.Trim() : name,
                    trimText ? title.Trim() : title,
                    trimText ? company.Trim() : company,
                    entry.BackgroundImagePath ?? string.Empty,
                    entry.OverlayImagePath ?? string.Empty);
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.NameText))
            .ToList();
    }

    private static string NormalizeHorizontalAlignment(string? alignment)
    {
        return alignment switch
        {
            "왼쪽" or "Left" => "왼쪽",
            "오른쪽" or "Right" => "오른쪽",
            _ => "가운데"
        };
    }

    private static string NormalizeVerticalAlignment(string? alignment)
    {
        return alignment switch
        {
            "위" or "Top" => "위",
            "아래" or "Bottom" => "아래",
            _ => "가운데"
        };
    }

    private void CenterTextPosition()
    {
        PositionX = GetCenterPosition(WidthMm);
        PositionY = GetCenterPosition(HeightMm);
    }

    private static double GetCenterPosition(double sizeMm)
    {
        return Math.Round(Math.Max(0, sizeMm) / 2.0, 2);
    }

    private void SetSelectedEntryImage(bool isBackground)
    {
        if (SelectedEntry is null)
        {
            ShowNotice("이미지를 넣을 항목 행을 먼저 선택해주세요.");
            return;
        }

        var path = imageFileService.PickImage();
        if (path is null)
        {
            StatusMessage = "이미지 선택이 취소되었습니다.";
            return;
        }

        if (isBackground)
        {
            SelectedEntry.BackgroundImagePath = path;
            StatusMessage = "선택한 항목의 배경 이미지를 변경했습니다.";
        }
        else
        {
            SelectedEntry.OverlayImagePath = path;
            StatusMessage = "선택한 항목의 이미지/로고를 변경했습니다.";
        }
    }

    private void PickColor(string targetName, string currentColor, Action<string> applyColor)
    {
        var selectedColor = colorDialogService.PickColor(currentColor);
        if (selectedColor is null)
        {
            StatusMessage = $"{targetName} 색상 선택이 취소되었습니다.";
            return;
        }

        applyColor(selectedColor);
        StatusMessage = $"{targetName} 색상을 {selectedColor}(으)로 변경했습니다.";
    }

    private void ShowNotice(string message)
    {
        StatusMessage = message;
        MessageBox.Show(message, "NamePlateStudio", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowError(string message)
    {
        StatusMessage = message;
        MessageBox.Show(message, "NamePlateStudio", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
