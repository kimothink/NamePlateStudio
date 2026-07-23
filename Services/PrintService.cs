using System.Globalization;
using System.IO;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NamePlateStudio.Helpers;
using NamePlateStudio.Models;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;
using PrintDialog = System.Windows.Controls.PrintDialog;
using Rectangle = System.Windows.Shapes.Rectangle;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Size = System.Windows.Size;

namespace NamePlateStudio.Services;

public sealed class PrintService
{
    private const double PdfDpi = 300;
    private readonly PrintLayoutService layoutService = new();

    public bool Print(
        NamePlateDesign design,
        int currentOutputPageIndex,
        int? selectedEntryIndex,
        out string message)
    {
        try
        {
            var layout = layoutService.CreateLayout(design);
            if (!layout.CanFit)
            {
                message = "현재 용지 크기, 여백, 간격 안에 명패가 들어가지 않습니다.";
                return false;
            }

            var dialog = new PrintDialog();
            dialog.PrintTicket.PageMediaSize = new PageMediaSize(layout.PageWidthPixels, layout.PageHeightPixels);
            dialog.PrintTicket.PageOrientation = design.IsLandscape ? PageOrientation.Landscape : PageOrientation.Portrait;
            var outputPageCount = GetOutputPageCount(design, layout);
            dialog.MinPage = 1;
            dialog.MaxPage = (uint)Math.Max(1, outputPageCount);
            dialog.UserPageRangeEnabled = outputPageCount > 1;
            dialog.CurrentPageEnabled = true;
            dialog.SelectedPagesEnabled = selectedEntryIndex.HasValue;
            if (design.IsDoubleSided)
            {
                dialog.PrintTicket.Duplexing = Duplexing.TwoSidedLongEdge;
            }

            if (dialog.ShowDialog() != true)
            {
                message = "인쇄가 취소되었습니다.";
                return false;
            }

            var printSelection = GetPrintSelection(
                dialog,
                design,
                layout,
                outputPageCount,
                currentOutputPageIndex,
                selectedEntryIndex);
            var document = CreateFixedDocument(design, layout, printSelection);
            dialog.PrintDocument(document.DocumentPaginator, "NamePlateStudio 명패");

            var selectedPageCount = printSelection.OutputPageIndexes.Count;
            if (design.IsDoubleSided)
            {
                message = $"선택한 인쇄면 {selectedPageCount}페이지를 양면 인쇄 작업으로 보냈습니다.";
                return true;
            }

            message = $"선택한 {selectedPageCount}페이지를 인쇄 작업으로 보냈습니다.";
            return true;
        }
        catch (PrintQueueException ex)
        {
            message = $"프린터를 사용할 수 없습니다. {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            message = $"인쇄 중 오류가 발생했습니다. {ex.Message}";
            return false;
        }
    }

    public void ShowPrintPreview(NamePlateDesign design)
    {
        var layout = layoutService.CreateLayout(design);
        var content = new StackPanel
        {
            Margin = new Thickness(24)
        };

        if (!layout.CanFit)
        {
            content.Children.Add(new TextBlock
            {
                Text = "현재 설정으로는 용지 안에 명패가 들어가지 않습니다.",
                Padding = new Thickness(16),
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            });
        }
        else
        {
            var outputPageCount = GetOutputPageCount(design, layout);
            foreach (var pageIndex in layout.PageIndexes)
            {
                var frontOutputPage = pageIndex * (design.IsDoubleSided ? 2 : 1) + 1;
                content.Children.Add(new TextBlock
                {
                    Text = $"인쇄 페이지 {frontOutputPage} / {outputPageCount} · 용지 {pageIndex + 1} 앞면",
                    Margin = new Thickness(0, pageIndex == 0 ? 0 : 22, 0, 8),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                });

                content.Children.Add(new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.DownOnly,
                    Child = CreatePaperPage(design, layout, pageIndex, showPaperGuide: true, isBackSide: false)
                });

                if (design.IsDoubleSided)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = $"인쇄 페이지 {frontOutputPage + 1} / {outputPageCount} · 용지 {pageIndex + 1} 뒷면",
                        Margin = new Thickness(0, 12, 0, 8),
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White
                    });
                    content.Children.Add(new Viewbox
                    {
                        Stretch = Stretch.Uniform,
                        StretchDirection = StretchDirection.DownOnly,
                        Child = CreatePaperPage(design, layout, pageIndex, showPaperGuide: true, isBackSide: true)
                    });
                }
            }
        }

        var window = new Window
        {
            Title = "인쇄 미리보기",
            Width = 980,
            Height = 860,
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    CreatePreviewHeader(design, layout),
                    new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Background = Brushes.DimGray,
                        Content = content
                    }
                }
            }
        };

        window.ShowDialog();
    }

    public bool SavePdf(NamePlateDesign design, out string message)
    {
        try
        {
            var layout = layoutService.CreateLayout(design);
            if (!layout.CanFit)
            {
                message = "현재 용지 크기, 여백, 간격 안에 명패가 들어가지 않습니다.";
                return false;
            }

            var dialog = new SaveFileDialog
            {
                Title = "PDF 저장",
                Filter = "PDF 문서 (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                FileName = $"명패-{DateTime.Now:yyyyMMdd-HHmm}.pdf"
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) != true)
            {
                message = "PDF 저장이 취소되었습니다.";
                return false;
            }

            return ExportPdf(design, dialog.FileName, out message);
        }
        catch (Exception ex)
        {
            message = $"PDF 저장 중 오류가 발생했습니다. {ex.Message}";
            return false;
        }
    }

    public bool ExportPdf(NamePlateDesign design, string path, out string message)
    {
        try
        {
            var layout = layoutService.CreateLayout(design);
            if (!layout.CanFit)
            {
                message = "현재 용지 크기, 여백, 간격 안에 명패가 들어가지 않습니다.";
                return false;
            }

            var pages = CreateOutputPages(design, layout).ToList();
            WritePdf(path, pages, layout.PageWidthMm, layout.PageHeightMm);
            message = $"PDF 저장 완료: {path}";
            return true;
        }
        catch (Exception ex)
        {
            message = $"PDF 저장 중 오류가 발생했습니다. {ex.Message}";
            return false;
        }
    }

    private static TextBlock CreatePreviewHeader(NamePlateDesign design, PrintLayout layout)
    {
        var orientation = design.IsLandscape ? "가로" : "세로";
        var summary = layout.CanFit
            ? $"{design.PaperSizeName} {orientation} {layout.PageWidthMm:0.##}mm x {layout.PageHeightMm:0.##}mm, 한 페이지 {layout.ItemsPerPage}개, 총 {layout.PageCount}페이지"
            : $"{design.PaperSizeName} {orientation} {layout.PageWidthMm:0.##}mm x {layout.PageHeightMm:0.##}mm";

        var header = new TextBlock
        {
            Text = summary,
            Padding = new Thickness(16, 12, 16, 10),
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
        };

        DockPanel.SetDock(header, Dock.Top);
        return header;
    }

    private static FixedDocument CreateFixedDocument(
        NamePlateDesign design,
        PrintLayout layout,
        PrintSelection selection)
    {
        var fixedDocument = new FixedDocument();
        fixedDocument.DocumentPaginator.PageSize = new Size(layout.PageWidthPixels, layout.PageHeightPixels);

        foreach (var page in CreateOutputPages(design, layout, selection.CopyNumber)
                     .Where((_, index) => selection.OutputPageIndexes.Contains(index)))
        {
            var fixedPage = new FixedPage
            {
                Width = layout.PageWidthPixels,
                Height = layout.PageHeightPixels,
                Background = Brushes.White
            };
            fixedPage.Children.Add(page);
            PreparePage(fixedPage, layout);

            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(fixedPage);
            fixedDocument.Pages.Add(pageContent);
        }

        return fixedDocument;
    }

    private static int GetOutputPageCount(NamePlateDesign design, PrintLayout layout)
        => layout.PageCount * (design.IsDoubleSided ? 2 : 1);

    private static PrintSelection GetPrintSelection(
        PrintDialog dialog,
        NamePlateDesign design,
        PrintLayout layout,
        int outputPageCount,
        int currentOutputPageIndex,
        int? selectedEntryIndex)
    {
        if (dialog.PageRangeSelection == PageRangeSelection.CurrentPage)
        {
            var currentPage = Math.Clamp(currentOutputPageIndex, 0, outputPageCount - 1);
            return new PrintSelection(new HashSet<int> { currentPage });
        }

        if (dialog.PageRangeSelection == PageRangeSelection.SelectedPages && selectedEntryIndex.HasValue)
        {
            var copyNumber = selectedEntryIndex.Value + 1;
            var placement = layout.Placements.FirstOrDefault(item => item.CopyNumber == copyNumber);
            if (placement is not null)
            {
                var sidesPerPage = design.IsDoubleSided ? 2 : 1;
                var firstOutputPage = placement.PageIndex * sidesPerPage;
                var outputPages = Enumerable.Range(firstOutputPage, sidesPerPage).ToHashSet();
                return new PrintSelection(outputPages, copyNumber);
            }
        }

        if (dialog.PageRangeSelection == PageRangeSelection.UserPages)
        {
            var firstPage = Math.Clamp(dialog.PageRange.PageFrom, 1, outputPageCount);
            var lastPage = Math.Clamp(dialog.PageRange.PageTo, firstPage, outputPageCount);
            var outputPages = Enumerable.Range(firstPage - 1, lastPage - firstPage + 1).ToHashSet();
            return new PrintSelection(outputPages);
        }

        return new PrintSelection(Enumerable.Range(0, outputPageCount).ToHashSet());
    }

    private static IEnumerable<Canvas> CreateOutputPages(NamePlateDesign design, PrintLayout layout, int? copyNumber = null)
    {
        foreach (var pageIndex in layout.PageIndexes)
        {
            yield return CreatePaperPage(design, layout, pageIndex, showPaperGuide: false, isBackSide: false, copyNumber);
            if (design.IsDoubleSided)
            {
                yield return CreatePaperPage(design, layout, pageIndex, showPaperGuide: false, isBackSide: true, copyNumber);
            }
        }
    }

    private static void PreparePage(FrameworkElement page, PrintLayout layout)
    {
        var size = new Size(layout.PageWidthPixels, layout.PageHeightPixels);
        page.Measure(size);
        page.Arrange(new Rect(size));
        page.UpdateLayout();
    }

    private static void WritePdf(string path, IReadOnlyList<Canvas> pages, double pageWidthMm, double pageHeightMm)
    {
        var renderedPages = pages.Select(RenderPageAsJpeg).ToList();
        var pageWidthPoints = pageWidthMm / 25.4 * 72.0;
        var pageHeightPoints = pageHeightMm / 25.4 * 72.0;
        var pageObjectNumbers = Enumerable.Range(0, pages.Count).Select(index => 3 + index * 3).ToArray();
        var objectCount = 2 + pages.Count * 3;

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var offsets = new long[objectCount + 1];
        WriteAscii(stream, "%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");

        WriteObject(stream, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
        WriteObject(stream, offsets, 2,
            $"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(' ', pageObjectNumbers.Select(number => $"{number} 0 R"))}] >>");

        for (var index = 0; index < renderedPages.Count; index++)
        {
            var pageObject = 3 + index * 3;
            var imageObject = pageObject + 1;
            var contentObject = pageObject + 2;
            var renderedPage = renderedPages[index];

            WriteObject(stream, offsets, pageObject,
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {FormatPdfNumber(pageWidthPoints)} {FormatPdfNumber(pageHeightPoints)}] " +
                $"/Resources << /XObject << /Im0 {imageObject} 0 R >> >> /Contents {contentObject} 0 R >>");

            WriteStreamObject(stream, offsets, imageObject,
                $"/Type /XObject /Subtype /Image /Width {renderedPage.PixelWidth} /Height {renderedPage.PixelHeight} " +
                "/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode",
                renderedPage.Data);

            var content = Encoding.ASCII.GetBytes(
                $"q {FormatPdfNumber(pageWidthPoints)} 0 0 {FormatPdfNumber(pageHeightPoints)} 0 0 cm /Im0 Do Q\n");
            WriteStreamObject(stream, offsets, contentObject, string.Empty, content);
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objectCount + 1}\n0000000000 65535 f \n");
        for (var objectNumber = 1; objectNumber <= objectCount; objectNumber++)
        {
            WriteAscii(stream, $"{offsets[objectNumber]:D10} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
    }

    private static RenderedPdfPage RenderPageAsJpeg(Canvas page)
    {
        var scale = PdfDpi / 96.0;
        var pixelWidth = Math.Max(1, (int)Math.Round(page.Width * scale));
        var pixelHeight = Math.Max(1, (int)Math.Round(page.Height * scale));
        var size = new Size(page.Width, page.Height);
        page.Measure(size);
        page.Arrange(new Rect(size));
        page.UpdateLayout();

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, PdfDpi, PdfDpi, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.White, null, new Rect(size));
            context.DrawRectangle(new VisualBrush(page), null, new Rect(size));
        }
        bitmap.Render(visual);

        var encoder = new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var memory = new MemoryStream();
        encoder.Save(memory);
        return new RenderedPdfPage(pixelWidth, pixelHeight, memory.ToArray());
    }

    private static void WriteObject(Stream stream, long[] offsets, int objectNumber, string body)
    {
        offsets[objectNumber] = stream.Position;
        WriteAscii(stream, $"{objectNumber} 0 obj\n{body}\nendobj\n");
    }

    private static void WriteStreamObject(Stream stream, long[] offsets, int objectNumber, string dictionary, byte[] data)
    {
        offsets[objectNumber] = stream.Position;
        WriteAscii(stream, $"{objectNumber} 0 obj\n<< {dictionary} /Length {data.Length} >>\nstream\n");
        stream.Write(data, 0, data.Length);
        WriteAscii(stream, "\nendstream\nendobj\n");
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string FormatPdfNumber(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed record RenderedPdfPage(int PixelWidth, int PixelHeight, byte[] Data);

    private sealed record PrintSelection(IReadOnlySet<int> OutputPageIndexes, int? CopyNumber = null);

    private static Canvas CreatePaperPage(
        NamePlateDesign design,
        PrintLayout layout,
        int pageIndex,
        bool showPaperGuide,
        bool isBackSide,
        int? copyNumber = null)
    {
        var page = new Canvas
        {
            Width = layout.PageWidthPixels,
            Height = layout.PageHeightPixels,
            Background = Brushes.White
        };

        if (showPaperGuide)
        {
            page.Children.Add(new Rectangle
            {
                Width = layout.PageWidthPixels,
                Height = layout.PageHeightPixels,
                Stroke = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                StrokeThickness = 1
            });

            page.Children.Add(new Rectangle
            {
                Width = layout.PrintableWidthPixels,
                Height = layout.PrintableHeightPixels,
                Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            });
            Canvas.SetLeft(page.Children[^1], layout.MarginPixels);
            Canvas.SetTop(page.Children[^1], layout.MarginPixels);
        }

        AddPlacedNamePlates(page.Children, design, layout, pageIndex, isBackSide, copyNumber);
        return page;
    }

    private static void AddPlacedNamePlates(
        UIElementCollection pageChildren,
        NamePlateDesign design,
        PrintLayout layout,
        int pageIndex,
        bool isBackSide,
        int? copyNumber)
    {
        foreach (var placement in layout.Placements.Where(item =>
                     item.PageIndex == pageIndex && (!copyNumber.HasValue || item.CopyNumber == copyNumber.Value)))
        {
            var plate = isBackSide
                ? CreateBackNamePlateElement(design, placement.Entry, showOutputGuide: true)
                : CreateNamePlateElement(design, placement.Entry, showOutputGuide: true);
            var left = isBackSide
                ? layout.PageWidthPixels - placement.LeftPixels - placement.WidthPixels
                : placement.LeftPixels;
            Canvas.SetLeft(plate, left);
            Canvas.SetTop(plate, placement.TopPixels);
            pageChildren.Add(plate);
        }
    }

    public static FrameworkElement CreateNamePlateElement(NamePlateDesign design, bool showOutputGuide)
    {
        return CreateNamePlateElement(design, new NamePlateEntry(design.NameText, design.TitleText, design.CompanyText), showOutputGuide);
    }

    public static FrameworkElement CreateNamePlateElement(NamePlateDesign design, NamePlateEntry entry, bool showOutputGuide)
    {
        var plateWidth = UnitConverter.MillimetersToPixels(design.WidthMm);
        var plateHeight = UnitConverter.MillimetersToPixels(design.HeightMm);
        var borderBrush = ColorHelper.ToBrush(design.BorderColor, Brushes.Black);
        var fontBrush = ColorHelper.ToBrush(design.FontColor, Brushes.Black);

        var root = new Grid
        {
            Width = plateWidth,
            Height = plateHeight,
            Background = ColorHelper.ToBrush(design.BackgroundColor, Brushes.White),
            ClipToBounds = true
        };

        var border = new Border
        {
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(Math.Max(0, design.BorderThickness)),
            Background = Brushes.Transparent,
            IsHitTestVisible = false
        };
        System.Windows.Controls.Panel.SetZIndex(border, 3);
        root.Children.Add(border);

        var canvas = new Canvas
        {
            Width = plateWidth,
            Height = plateHeight,
            ClipToBounds = true
        };
        root.Children.Add(canvas);

        if (design.HasDecorationLine)
        {
            AddDecorationLines(canvas, plateWidth, plateHeight, borderBrush);
        }

        AddOverlayImage(canvas, entry, design);
        AddAnchoredText(canvas, entry.CompanyText, design, Math.Max(10, design.FontSize * 0.36), -design.FontSize * 0.95, fontBrush, FontWeights.Normal);
        AddAnchoredText(canvas, entry.NameText, design, design.FontSize, 0, fontBrush, design.IsBold ? FontWeights.Bold : FontWeights.Normal);
        AddAnchoredText(canvas, entry.TitleText, design, Math.Max(10, design.FontSize * 0.42), design.FontSize * 0.82, fontBrush, FontWeights.Normal);

        if (showOutputGuide)
        {
            var outputGuide = new Rectangle
            {
                Stroke = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };
            System.Windows.Controls.Panel.SetZIndex(outputGuide, 4);
            root.Children.Add(outputGuide);
        }

        return root;
    }

    public static FrameworkElement CreateBackNamePlateElement(NamePlateDesign design, NamePlateEntry entry, bool showOutputGuide)
    {
        var plateWidth = UnitConverter.MillimetersToPixels(design.WidthMm);
        var plateHeight = UnitConverter.MillimetersToPixels(design.HeightMm);
        var root = new Canvas
        {
            Width = plateWidth,
            Height = plateHeight,
            Background = ColorHelper.ToBrush(design.BackBackgroundColor, Brushes.White),
            ClipToBounds = true
        };

        var backImage = ImageHelper.CreateImageSource(entry.BackImagePath);
        var contentPanel = CreateBackContentPanel(design, entry);

        var contentBorder = new Border
        {
            Width = plateWidth,
            Height = plateHeight,
            BorderBrush = ColorHelper.ToBrush(design.BackBorderColor, Brushes.Black),
            BorderThickness = new Thickness(Math.Max(0, design.BorderThickness)),
            Padding = new Thickness(UnitConverter.MillimetersToPixels(6)),
            Child = contentPanel
        };
        Canvas.SetZIndex(contentBorder, 1);
        root.Children.Add(contentBorder);

        if (backImage is not null)
        {
            var image = new Image
            {
                Source = backImage,
                Width = UnitConverter.MillimetersToPixels(Math.Max(1, entry.BackImageWidthMm)),
                Height = UnitConverter.MillimetersToPixels(Math.Max(1, entry.BackImageHeightMm)),
                Stretch = Stretch.Uniform,
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
                RenderTransform = new RotateTransform(entry.BackImageRotation)
            };
            root.Children.Add(image);
            Canvas.SetZIndex(image, 0);
            Canvas.SetLeft(image, UnitConverter.MillimetersToPixels(entry.BackImageX));
            Canvas.SetTop(image, UnitConverter.MillimetersToPixels(entry.BackImageY));
        }

        if (showOutputGuide)
        {
            var outputGuide = new Rectangle
            {
                Width = plateWidth,
                Height = plateHeight,
                Stroke = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };
            root.Children.Add(outputGuide);
            Canvas.SetZIndex(outputGuide, 2);
        }

        return root;
    }

    private static StackPanel CreateBackContentPanel(NamePlateDesign design, NamePlateEntry entry)
    {
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        if (!string.IsNullOrWhiteSpace(entry.BackContent))
        {
            panel.Children.Add(new TextBlock
            {
                Text = entry.BackContent, FontFamily = new FontFamily(design.FontFamily),
                FontSize = Math.Max(8, design.BackFontSize), Foreground = ColorHelper.ToBrush(design.BackFontColor, Brushes.Black),
                TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8)
            });
        }
        return panel;
    }

    private static void AddDecorationLines(Canvas canvas, double plateWidth, double plateHeight, Brush stroke)
    {
        var startX = plateWidth * 0.08;
        var endX = plateWidth * 0.92;

        foreach (var y in new[] { plateHeight * 0.24, plateHeight * 0.76 })
        {
            var line = new Line
            {
                X1 = startX,
                X2 = endX,
                Y1 = y,
                Y2 = y,
                Stroke = stroke,
                StrokeThickness = 1.5,
                Opacity = 0.75
            };
            Canvas.SetZIndex(line, 2);
            canvas.Children.Add(line);
        }
    }

    private static void AddOverlayImage(Canvas canvas, NamePlateEntry entry, NamePlateDesign design)
    {
        var imageSource = ImageHelper.CreateImageSource(entry.OverlayImagePath);
        if (imageSource is null || design.OverlayImageWidth <= 0 || design.OverlayImageHeight <= 0)
        {
            return;
        }

        var image = new Image
        {
            Source = imageSource,
            Width = UnitConverter.MillimetersToPixels(design.OverlayImageWidth),
            Height = UnitConverter.MillimetersToPixels(design.OverlayImageHeight),
            Stretch = Stretch.UniformToFill,
            RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
            RenderTransform = new RotateTransform(design.OverlayImageRotation),
            IsHitTestVisible = false
        };

        Canvas.SetLeft(image, UnitConverter.MillimetersToPixels(design.OverlayImageX));
        Canvas.SetTop(image, UnitConverter.MillimetersToPixels(design.OverlayImageY));
        Canvas.SetZIndex(image, 0);
        canvas.Children.Add(image);
    }

    private static void AddAnchoredText(Canvas canvas, string? text, NamePlateDesign design, double fontSize, double offsetY, Brush foreground, FontWeight weight)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var textBlock = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(design.FontFamily),
            FontSize = fontSize,
            Foreground = foreground,
            FontWeight = weight,
            FontStyle = design.IsItalic ? FontStyles.Italic : FontStyles.Normal,
            TextAlignment = ToTextAlignment(design.HorizontalTextAlignment)
        };

        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var anchorX = UnitConverter.MillimetersToPixels(design.PositionX);
        var anchorY = UnitConverter.MillimetersToPixels(design.PositionY) + offsetY;
        Canvas.SetLeft(textBlock, GetHorizontalLeft(anchorX, textBlock.DesiredSize.Width, design.HorizontalTextAlignment));
        Canvas.SetTop(textBlock, GetVerticalTop(anchorY, textBlock.DesiredSize.Height, offsetY == 0 ? design.VerticalTextAlignment : "가운데"));
        Canvas.SetZIndex(textBlock, 2);
        canvas.Children.Add(textBlock);
    }

    private static double GetHorizontalLeft(double anchorX, double desiredWidth, string alignment)
    {
        return alignment switch
        {
            "왼쪽" or "Left" => anchorX,
            "오른쪽" or "Right" => anchorX - desiredWidth,
            _ => anchorX - desiredWidth / 2.0
        };
    }

    private static double GetVerticalTop(double anchorY, double desiredHeight, string alignment)
    {
        return alignment switch
        {
            "위" or "Top" => anchorY,
            "아래" or "Bottom" => anchorY - desiredHeight,
            _ => anchorY - desiredHeight / 2.0
        };
    }

    private static TextAlignment ToTextAlignment(string alignment)
    {
        return alignment switch
        {
            "왼쪽" or "Left" => TextAlignment.Left,
            "오른쪽" or "Right" => TextAlignment.Right,
            _ => TextAlignment.Center
        };
    }
}
