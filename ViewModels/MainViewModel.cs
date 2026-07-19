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
using NamePlateStudio.Views;
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
    private bool isSynchronizingImages;

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
    private string backFontColor = "#111827";

    [ObservableProperty]
    private string backBackgroundColor = "#FFFFFF";

    [ObservableProperty]
    private string backBorderColor = "#1F2937";

    [ObservableProperty]
    private bool matchBackBackgroundToImage;

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
    private double overlayImageRotation;

    [ObservableProperty]
    private bool applyFrontImagesToAllEntries;

    [ObservableProperty]
    private bool applyBackImagesToAllEntries;

    [ObservableProperty]
    private bool lockImageAspectRatio = true;

    [ObservableProperty]
    private PaperSizeOption? selectedPaperSize;

    [ObservableProperty]
    private double paperWidthMm = 210;

    [ObservableProperty]
    private double paperHeightMm = 297;

    [ObservableProperty]
    private bool isLandscape;

    [ObservableProperty]
    private bool isDoubleSided;

    [ObservableProperty]
    private double backFontSize = 20;

    [ObservableProperty]
    private string bulkBackContent = string.Empty;

    [ObservableProperty]
    private int previewPageIndex;

    [ObservableProperty]
    private int previewPageCount = 1;

    [ObservableProperty]
    private bool isPreviewingBack;

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

    public double OverlayImageXPixels => UnitConverter.MillimetersToPixels(OverlayImageX);

    public double OverlayImageYPixels => UnitConverter.MillimetersToPixels(OverlayImageY);

    public double OverlayImageWidthPixels => UnitConverter.MillimetersToPixels(Math.Max(0, OverlayImageWidth));

    public double OverlayImageHeightPixels => UnitConverter.MillimetersToPixels(Math.Max(0, OverlayImageHeight));

    public string PlateSizeText => $"{WidthMm:0.##} mm x {HeightMm:0.##} mm";

    public string PaperSizeText => $"{EffectivePaperWidthMm:0.##} mm x {EffectivePaperHeightMm:0.##} mm";

    public string EntryCountText => $"{GetPrintableEntries(trimText: false).Count}명";

    public string ZoomPercentText => $"{Zoom * 100:0}%";

    public string PreviewPageText => $"{PreviewPageIndex + 1} / {Math.Max(1, PreviewPageCount)} 페이지";

    public bool IsFrontPreview => !IsPreviewingBack;

    public Brush FontBrush => ColorHelper.ToBrush(FontColor, Brushes.Black);

    public Brush BackgroundBrush => ColorHelper.ToBrush(BackgroundColor, Brushes.White);

    public Brush BorderBrush => ColorHelper.ToBrush(BorderColor, Brushes.Black);

    public Brush BackFontBrush => ColorHelper.ToBrush(BackFontColor, Brushes.Black);

    public Brush BackBackgroundBrush => ColorHelper.ToBrush(BackBackgroundColor, Brushes.White);

    public Brush BackBorderBrush => ColorHelper.ToBrush(BackBorderColor, Brushes.Black);

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
    private void ApplyBackContentToAll()
    {
        if (Entries.Count == 0)
        {
            ShowNotice("일괄 적용할 명찰 항목이 없습니다.");
            return;
        }

        foreach (var entry in Entries)
        {
            entry.BackContent = BulkBackContent ?? string.Empty;
        }

        IsDoubleSided = true;
        StatusMessage = $"뒷면 내용을 {Entries.Count}개 명찰에 일괄 적용했습니다.";
    }

    [RelayCommand]
    private void OpenBackEditor(object? parameter)
    {
        if (parameter is not NamePlateEntry entry)
        {
            return;
        }

        var editor = new BackEditorWindow(entry, Entries)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (editor.ShowDialog() == true)
        {
            IsDoubleSided = true;
            RefreshPreviewProperties();
            StatusMessage = editor.AppliedToAll
                ? "뒷면 편집 내용을 모든 명찰에 적용했습니다."
                : $"{entry.NameText} 명찰의 뒷면 내용을 저장했습니다.";
        }
    }

    [RelayCommand]
    private void SelectEntryOverlayImage()
    {
        SetSelectedEntryImage();
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
        StatusMessage = "선택한 항목의 앞면 그림을 제거했습니다.";
    }

    [RelayCommand]
    private void ScaleFrontImage(string? factorText)
    {
        if (!TryReadScaleFactor(factorText, out var factor))
        {
            return;
        }

        ScaleFrontImagePlacement(factor);
        StatusMessage = $"앞면 그림 크기를 {factor * 100:0}%로 조절했습니다.";
    }

    [RelayCommand]
    private void FitFrontImage()
    {
        OverlayImageX = 0;
        OverlayImageY = 0;
        OverlayImageWidth = Math.Max(1, WidthMm);
        OverlayImageHeight = Math.Max(1, HeightMm);
        OverlayImageRotation = 0;
        StatusMessage = "앞면 그림을 배경처럼 명패 전체에 꽉 채웠습니다.";
    }

    [RelayCommand]
    private void SelectEntryBackImage()
    {
        if (SelectedEntry is null)
        {
            ShowNotice("뒷면 이미지를 넣을 명찰을 먼저 선택해주세요.");
            return;
        }

        var path = imageFileService.PickImage();
        if (path is null)
        {
            StatusMessage = "뒷면 이미지 선택을 취소했습니다.";
            return;
        }

        SelectedEntry.BackImagePath = path;
        FitBackImageToOriginal(SelectedEntry);
        if (MatchBackBackgroundToImage)
        {
            ApplyBackBackgroundFromImage();
        }
        IsDoubleSided = true;
        StatusMessage = "뒷면 이미지를 원본 전체가 보이는 크기로 넣었습니다.";
    }

    [RelayCommand]
    private void ClearEntryBackImage()
    {
        if (SelectedEntry is null)
        {
            ShowNotice("뒷면 이미지를 삭제할 명찰을 먼저 선택해주세요.");
            return;
        }

        SelectedEntry.BackImagePath = string.Empty;
        StatusMessage = "선택한 명찰의 뒷면 이미지를 삭제했습니다.";
    }

    [RelayCommand]
    private void ScaleBackImage(string? factorText)
    {
        if (SelectedEntry is null || !TryReadScaleFactor(factorText, out var factor))
        {
            return;
        }

        ScaleBackImagePlacement(SelectedEntry, factor);
        StatusMessage = $"뒷면 그림 크기를 {factor * 100:0}%로 조절했습니다.";
    }

    [RelayCommand]
    private void RotateBackImage(string? degreesText)
    {
        if (SelectedEntry is null || !TryReadDegrees(degreesText, out var degrees))
        {
            return;
        }

        SelectedEntry.BackImageRotation = NormalizeDegrees(SelectedEntry.BackImageRotation + degrees);
        FitBackImageToOriginal(SelectedEntry);
        StatusMessage = $"뒷면 그림을 {Math.Abs(degrees):0}도 {(degrees < 0 ? "왼쪽" : "오른쪽")}으로 회전했습니다.";
    }

    [RelayCommand]
    private void ResetBackImageRotation()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        SelectedEntry.BackImageRotation = 0;
        FitBackImageToOriginal(SelectedEntry);
        StatusMessage = "뒷면 그림 회전을 초기화했습니다.";
    }

    [RelayCommand]
    private void FitBackImage()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        if (!FitBackImageToOriginal(SelectedEntry))
        {
            StatusMessage = "원본 크기를 맞출 뒷면 그림을 먼저 선택해주세요.";
            return;
        }

        StatusMessage = "뒷면 그림의 원본 전체가 보이도록 크기와 위치를 맞췄습니다.";
    }

    [RelayCommand]
    private void PreviousPreviewPage()
    {
        if (PreviewPageIndex > 0)
        {
            PreviewPageIndex--;
        }
    }

    [RelayCommand]
    private void NextPreviewPage()
    {
        if (PreviewPageIndex + 1 < PreviewPageCount)
        {
            PreviewPageIndex++;
        }
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
            StatusMessage = path is null ? "양식 저장이 취소되었습니다." : $"양식 저장 완료: {path}";
        }
        catch (Exception ex)
        {
            ShowError($"양식 저장에 실패했습니다.\n{ex.Message}");
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
                StatusMessage = "양식 불러오기가 취소되었습니다.";
                return;
            }

            ApplyDesign(design);
            StatusMessage = $"양식 불러오기 완료: {path}";
        }
        catch (Exception ex)
        {
            ShowError($"양식 불러오기에 실패했습니다.\n{ex.Message}");
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

    [RelayCommand]
    private void SelectBackFontColor()
    {
        PickColor("뒷면 글자", BackFontColor, selectedColor => BackFontColor = selectedColor);
    }

    [RelayCommand]
    private void SelectBackBackgroundColor()
    {
        PickColor("뒷면 배경", BackBackgroundColor, selectedColor =>
        {
            MatchBackBackgroundToImage = false;
            BackBackgroundColor = selectedColor;
        });
    }

    [RelayCommand]
    private void SelectBackBorderColor()
    {
        PickColor("뒷면 테두리", BackBorderColor, selectedColor => BackBorderColor = selectedColor);
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

    partial void OnBackFontColorChanged(string value) => RefreshPreviewProperties();

    partial void OnBackBackgroundColorChanged(string value) => RefreshPreviewProperties();

    partial void OnBackBorderColorChanged(string value) => RefreshPreviewProperties();

    partial void OnMatchBackBackgroundToImageChanged(bool value)
    {
        if (value)
        {
            ApplyBackBackgroundFromImage();
        }
    }

    partial void OnSelectedEntryChanged(NamePlateEntry? value)
    {
        if (MatchBackBackgroundToImage && value is not null && !string.IsNullOrWhiteSpace(value.BackImagePath))
        {
            ApplyBackBackgroundFromImage();
        }

    }

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

    partial void OnOverlayImageRotationChanged(double value) => RefreshPreviewProperties();

    partial void OnApplyFrontImagesToAllEntriesChanged(bool value)
    {
        if (!value || SelectedEntry is null || Entries.Count == 0)
        {
            return;
        }

        SynchronizeFrontImagesFrom(SelectedEntry);
        StatusMessage = $"현재 앞면 그림 설정을 {Entries.Count}개 명찰에 적용했습니다.";
    }

    partial void OnApplyBackImagesToAllEntriesChanged(bool value)
    {
        if (!value || SelectedEntry is null || Entries.Count == 0)
        {
            return;
        }

        SynchronizeBackImagesFrom(SelectedEntry);
        StatusMessage = $"현재 뒷면 그림 설정을 {Entries.Count}개 명찰에 적용했습니다.";
    }

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

    partial void OnIsDoubleSidedChanged(bool value)
    {
        if (!value)
        {
            IsPreviewingBack = false;
        }
        RefreshPreviewProperties();
    }

    partial void OnBackFontSizeChanged(double value) => RefreshPreviewProperties();

    partial void OnPreviewPageIndexChanged(int value)
    {
        OnPropertyChanged(nameof(PreviewPageText));
        RebuildPreviewPlacements();
    }

    partial void OnPreviewPageCountChanged(int value) => OnPropertyChanged(nameof(PreviewPageText));

    partial void OnIsPreviewingBackChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFrontPreview));
        RefreshPreviewProperties();
    }

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

        if (entries.Any(entry => !string.IsNullOrWhiteSpace(entry.BackImagePath)
            && (entry.BackImageWidthMm <= 0 || entry.BackImageHeightMm <= 0)))
        {
            ShowNotice("뒷면 그림의 가로/세로 크기는 0보다 커야 합니다.");
            return false;
        }

        if (PageMarginMm < 0 || HorizontalGapMm < 0 || VerticalGapMm < 0)
        {
            ShowNotice("여백과 간격은 0 이상이어야 합니다.");
            return false;
        }

        if (OverlayImageWidth <= 0 || OverlayImageHeight <= 0)
        {
            ShowNotice("이미지 가로/세로 크기는 0보다 커야 합니다.");
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

        if (!ColorHelper.TryCreateBrush(BackFontColor, out _) ||
            !ColorHelper.TryCreateBrush(BackBackgroundColor, out _) ||
            !ColorHelper.TryCreateBrush(BackBorderColor, out _))
        {
            ShowNotice("뒷면 색상을 다시 선택해주세요.");
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
            MatchFrontBackgroundToImage = false,
            BorderColor = BorderColor,
            BackFontColor = BackFontColor,
            BackBackgroundColor = BackBackgroundColor,
            BackBorderColor = BackBorderColor,
            MatchBackBackgroundToImage = MatchBackBackgroundToImage,
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
            OverlayImageRotation = OverlayImageRotation,
            ApplyFrontImagesToAllEntries = ApplyFrontImagesToAllEntries,
            ApplyBackImagesToAllEntries = ApplyBackImagesToAllEntries,
            LockImageAspectRatio = LockImageAspectRatio,
            PaperSizeName = SelectedPaperSize?.Name ?? "사용자 지정",
            PaperWidthMm = PaperWidthMm,
            PaperHeightMm = PaperHeightMm,
            IsLandscape = IsLandscape,
            IsDoubleSided = IsDoubleSided,
            BackFontSize = BackFontSize,
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
        BackFontColor = string.IsNullOrWhiteSpace(design.BackFontColor) ? "#111827" : design.BackFontColor;
        BackBackgroundColor = string.IsNullOrWhiteSpace(design.BackBackgroundColor) ? "#FFFFFF" : design.BackBackgroundColor;
        BackBorderColor = string.IsNullOrWhiteSpace(design.BackBorderColor) ? "#1F2937" : design.BackBorderColor;
        BorderThickness = Math.Max(0, design.BorderThickness);
        PositionX = design.PositionX;
        PositionY = design.PositionY;
        IsBold = design.IsBold;
        IsItalic = design.IsItalic;
        HorizontalTextAlignment = NormalizeHorizontalAlignment(design.HorizontalTextAlignment);
        VerticalTextAlignment = NormalizeVerticalAlignment(design.VerticalTextAlignment);
        HasDecorationLine = design.HasDecorationLine;
        OverlayImageX = design.OverlayImageX;
        OverlayImageY = design.OverlayImageY;
        OverlayImageWidth = design.OverlayImageWidth <= 0 ? 18 : design.OverlayImageWidth;
        OverlayImageHeight = design.OverlayImageHeight <= 0 ? 18 : design.OverlayImageHeight;
        OverlayImageRotation = NormalizeDegrees(design.OverlayImageRotation);
        LockImageAspectRatio = design.LockImageAspectRatio;

        var entries = design.Entries is { Count: > 0 }
            ? design.Entries
            : [new NamePlateEntry(design.NameText, design.TitleText, design.CompanyText)];
        ApplyEntries(entries);
        ApplyFrontImagesToAllEntries = design.ApplyFrontImagesToAllEntries;
        ApplyBackImagesToAllEntries = design.ApplyBackImagesToAllEntries;
        MatchBackBackgroundToImage = design.MatchBackBackgroundToImage;

        var paperOption = PaperSizes.FirstOrDefault(option => option.Name == design.PaperSizeName)
            ?? PaperSizes.First(option => option.IsCustom);
        SelectedPaperSize = paperOption;
        PaperWidthMm = design.PaperWidthMm <= 0 ? paperOption.WidthMm : design.PaperWidthMm;
        PaperHeightMm = design.PaperHeightMm <= 0 ? paperOption.HeightMm : design.PaperHeightMm;
        IsLandscape = design.IsLandscape;
        IsDoubleSided = design.IsDoubleSided;
        BackFontSize = design.BackFontSize <= 0 ? 20 : design.BackFontSize;
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
                string.Empty,
                ResolveFrontImagePath(entry),
                entry.BackContent ?? string.Empty,
                entry.BackImagePath ?? string.Empty,
                entry.BackImageWidthMm,
                entry.BackImageHeightMm,
                entry.BackTableRows,
                entry.BackTableColumns,
                entry.BackTableCells,
                entry.BackImageX,
                entry.BackImageY,
                entry.BackImageRotation);
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
                if (SelectedEntry is not null && !ReferenceEquals(entry, SelectedEntry))
                {
                    if (ApplyFrontImagesToAllEntries)
                    {
                        CopyFrontImages(SelectedEntry, entry);
                    }
                    if (ApplyBackImagesToAllEntries)
                    {
                        CopyBackImages(SelectedEntry, entry);
                    }
                }
            }
        }

        RefreshPreviewProperties();
    }

    private void EntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!isSynchronizingImages && sender is NamePlateEntry changedEntry &&
            ReferenceEquals(changedEntry, SelectedEntry))
        {
            if (ApplyFrontImagesToAllEntries && IsFrontImageProperty(e.PropertyName))
            {
                SynchronizeFrontImagesFrom(changedEntry);
            }
            else if (ApplyBackImagesToAllEntries && IsBackImageProperty(e.PropertyName))
            {
                SynchronizeBackImagesFrom(changedEntry);
            }
        }

        if (sender is NamePlateEntry entry && IsBackSideProperty(e.PropertyName) && HasBackSideContent(entry))
        {
            IsDoubleSided = true;
        }

        RefreshPreviewProperties();
    }

    private static bool IsBackSideProperty(string? propertyName)
    {
        return propertyName is nameof(NamePlateEntry.BackContent)
            or nameof(NamePlateEntry.BackImagePath)
            or nameof(NamePlateEntry.BackImageX)
            or nameof(NamePlateEntry.BackImageY)
            or nameof(NamePlateEntry.BackImageWidthMm)
            or nameof(NamePlateEntry.BackImageHeightMm)
            or nameof(NamePlateEntry.BackImageRotation);
    }

    private static bool HasBackSideContent(NamePlateEntry entry)
    {
        return !string.IsNullOrWhiteSpace(entry.BackContent)
            || !string.IsNullOrWhiteSpace(entry.BackImagePath);
    }

    private static bool IsFrontImageProperty(string? propertyName)
    {
        return propertyName is nameof(NamePlateEntry.OverlayImagePath);
    }

    private static bool IsBackImageProperty(string? propertyName)
    {
        return propertyName is nameof(NamePlateEntry.BackImagePath)
            or nameof(NamePlateEntry.BackImageX)
            or nameof(NamePlateEntry.BackImageY)
            or nameof(NamePlateEntry.BackImageWidthMm)
            or nameof(NamePlateEntry.BackImageHeightMm)
            or nameof(NamePlateEntry.BackImageRotation);
    }

    private void SynchronizeFrontImagesFrom(NamePlateEntry source)
    {
        isSynchronizingImages = true;
        try
        {
            foreach (var entry in Entries.Where(entry => !ReferenceEquals(entry, source)))
            {
                CopyFrontImages(source, entry);
            }
        }
        finally
        {
            isSynchronizingImages = false;
        }
    }

    private void SynchronizeBackImagesFrom(NamePlateEntry source)
    {
        isSynchronizingImages = true;
        try
        {
            foreach (var entry in Entries.Where(entry => !ReferenceEquals(entry, source)))
            {
                CopyBackImages(source, entry);
            }
        }
        finally
        {
            isSynchronizingImages = false;
        }
    }

    private static void CopyFrontImages(NamePlateEntry source, NamePlateEntry target)
    {
        target.BackgroundImagePath = string.Empty;
        target.OverlayImagePath = source.OverlayImagePath;
    }

    private static void CopyBackImages(NamePlateEntry source, NamePlateEntry target)
    {
        target.BackImagePath = source.BackImagePath;
        target.BackImageX = source.BackImageX;
        target.BackImageY = source.BackImageY;
        target.BackImageWidthMm = source.BackImageWidthMm;
        target.BackImageHeightMm = source.BackImageHeightMm;
        target.BackImageRotation = source.BackImageRotation;
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
        OnPropertyChanged(nameof(BackFontBrush));
        OnPropertyChanged(nameof(BackBackgroundBrush));
        OnPropertyChanged(nameof(BackBorderBrush));
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
            PreviewPageCount = 1;
            if (PreviewPageIndex != 0)
            {
                PreviewPageIndex = 0;
                return;
            }
            LayoutSummaryText = "명패 내용 목록에 표시명을 입력해주세요.";
            return;
        }

        var layout = layoutService.CreateLayout(CreateDesignSnapshot());
        PreviewPageCount = Math.Max(1, layout.PageCount);
        if (PreviewPageIndex >= PreviewPageCount)
        {
            PreviewPageIndex = PreviewPageCount - 1;
            return;
        }

        foreach (var placement in layout.Placements.Where(item => item.PageIndex == PreviewPageIndex))
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
                    string.Empty,
                    ResolveFrontImagePath(entry),
                    trimText ? (entry.BackContent ?? string.Empty).Trim() : entry.BackContent ?? string.Empty,
                    entry.BackImagePath ?? string.Empty,
                    entry.BackImageWidthMm,
                    entry.BackImageHeightMm,
                    entry.BackTableRows,
                    entry.BackTableColumns,
                    entry.BackTableCells,
                    entry.BackImageX,
                    entry.BackImageY,
                    entry.BackImageRotation);
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

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static bool TryReadDegrees(string? text, out double degrees)
        => double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out degrees);

    private static bool TryReadScaleFactor(string? text, out double factor)
        => double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out factor)
            && factor > 0;

    private void ScaleFrontImagePlacement(double factor)
    {
        var width = Math.Max(1, OverlayImageWidth * factor);
        var height = Math.Max(1, OverlayImageHeight * factor);
        var centerX = OverlayImageX + OverlayImageWidth / 2;
        var centerY = OverlayImageY + OverlayImageHeight / 2;
        var boundedFactor = Math.Min(1,
            Math.Min(Math.Max(1, WidthMm) / width, Math.Max(1, HeightMm) / height));
        if (width > WidthMm || height > HeightMm)
        {
            width *= boundedFactor;
            height *= boundedFactor;
        }

        OverlayImageWidth = width;
        OverlayImageHeight = height;
        OverlayImageX = Math.Clamp(centerX - width / 2, 0, Math.Max(0, WidthMm - width));
        OverlayImageY = Math.Clamp(centerY - height / 2, 0, Math.Max(0, HeightMm - height));
    }

    private void ScaleBackImagePlacement(NamePlateEntry entry, double factor)
    {
        var width = Math.Max(1, entry.BackImageWidthMm * factor);
        var height = Math.Max(1, entry.BackImageHeightMm * factor);
        var centerX = entry.BackImageX + entry.BackImageWidthMm / 2;
        var centerY = entry.BackImageY + entry.BackImageHeightMm / 2;
        var boundedFactor = Math.Min(1,
            Math.Min(Math.Max(1, WidthMm) / width, Math.Max(1, HeightMm) / height));
        if (width > WidthMm || height > HeightMm)
        {
            width *= boundedFactor;
            height *= boundedFactor;
        }

        entry.BackImageWidthMm = width;
        entry.BackImageHeightMm = height;
        entry.BackImageX = Math.Clamp(centerX - width / 2, 0, Math.Max(0, WidthMm - width));
        entry.BackImageY = Math.Clamp(centerY - height / 2, 0, Math.Max(0, HeightMm - height));
    }

    private bool FitBackImageToOriginal(NamePlateEntry entry)
    {
        var imageSource = ImageHelper.CreateImageSource(entry.BackImagePath);
        if (imageSource is null || imageSource.Width <= 0 || imageSource.Height <= 0 || WidthMm <= 0 || HeightMm <= 0)
        {
            return false;
        }

        var radians = entry.BackImageRotation * Math.PI / 180.0;
        var absoluteCos = Math.Abs(Math.Cos(radians));
        var absoluteSin = Math.Abs(Math.Sin(radians));
        var rotatedSourceWidth = imageSource.Width * absoluteCos + imageSource.Height * absoluteSin;
        var rotatedSourceHeight = imageSource.Width * absoluteSin + imageSource.Height * absoluteCos;
        var scale = Math.Min(WidthMm / rotatedSourceWidth, HeightMm / rotatedSourceHeight);
        if (!double.IsFinite(scale) || scale <= 0)
        {
            return false;
        }

        var width = imageSource.Width * scale;
        var height = imageSource.Height * scale;
        entry.BackImageWidthMm = Math.Round(width, 2);
        entry.BackImageHeightMm = Math.Round(height, 2);
        entry.BackImageX = Math.Round((WidthMm - width) / 2, 2);
        entry.BackImageY = Math.Round((HeightMm - height) / 2, 2);
        return true;
    }

    private void ApplyBackBackgroundFromImage()
    {
        if (SelectedEntry is null || string.IsNullOrWhiteSpace(SelectedEntry.BackImagePath))
        {
            StatusMessage = "배경색을 맞출 뒷면 그림을 먼저 선택해주세요.";
            return;
        }

        if (!ImageColorHelper.TryGetRepresentativeColor(SelectedEntry.BackImagePath, out var colorHex))
        {
            StatusMessage = "뒷면 그림에서 배경색을 추출하지 못했습니다.";
            return;
        }

        BackBackgroundColor = colorHex;
        StatusMessage = $"뒷면 배경색을 그림의 대표 색상 {colorHex}(으)로 적용했습니다.";
    }

    private void SetSelectedEntryImage()
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

        SelectedEntry.BackgroundImagePath = string.Empty;
        SelectedEntry.OverlayImagePath = path;
        StatusMessage = "선택한 항목의 앞면 그림을 변경했습니다.";
    }

    // 예전 JSON의 배경 이미지는 통합된 앞면 그림 레이어로 자동 승계합니다.
    private static string ResolveFrontImagePath(NamePlateEntry entry)
    {
        return !string.IsNullOrWhiteSpace(entry.OverlayImagePath)
            ? entry.OverlayImagePath
            : entry.BackgroundImagePath ?? string.Empty;
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
