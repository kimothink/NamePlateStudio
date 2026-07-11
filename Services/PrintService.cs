using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
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
using Size = System.Windows.Size;

namespace NamePlateStudio.Services;

public sealed class PrintService
{
    private readonly PrintLayoutService layoutService = new();

    public bool Print(NamePlateDesign design, out string message)
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

            if (dialog.ShowDialog() != true)
            {
                message = "인쇄가 취소되었습니다.";
                return false;
            }

            var document = CreateFixedDocument(design, layout);
            dialog.PrintDocument(document.DocumentPaginator, "NamePlateStudio 명패");

            message = $"{layout.PageCount}페이지, 총 {layout.Placements.Count}명 명패를 인쇄 작업으로 보냈습니다.";
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
            foreach (var pageIndex in layout.PageIndexes)
            {
                content.Children.Add(new TextBlock
                {
                    Text = $"{pageIndex + 1} / {layout.PageCount} 페이지",
                    Margin = new Thickness(0, pageIndex == 0 ? 0 : 22, 0, 8),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                });

                content.Children.Add(new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.DownOnly,
                    Child = CreatePaperPage(design, layout, pageIndex, showPaperGuide: true)
                });
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

    private static FixedDocument CreateFixedDocument(NamePlateDesign design, PrintLayout layout)
    {
        var fixedDocument = new FixedDocument();
        fixedDocument.DocumentPaginator.PageSize = new Size(layout.PageWidthPixels, layout.PageHeightPixels);

        foreach (var pageIndex in layout.PageIndexes)
        {
            var page = new FixedPage
            {
                Width = layout.PageWidthPixels,
                Height = layout.PageHeightPixels,
                Background = Brushes.White
            };

            AddPlacedNamePlates(page.Children, design, layout, pageIndex);

            page.Measure(new Size(layout.PageWidthPixels, layout.PageHeightPixels));
            page.Arrange(new Rect(new Size(layout.PageWidthPixels, layout.PageHeightPixels)));
            page.UpdateLayout();

            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(page);
            fixedDocument.Pages.Add(pageContent);
        }

        return fixedDocument;
    }

    private static Canvas CreatePaperPage(NamePlateDesign design, PrintLayout layout, int pageIndex, bool showPaperGuide)
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

        AddPlacedNamePlates(page.Children, design, layout, pageIndex);
        return page;
    }

    private static void AddPlacedNamePlates(UIElementCollection pageChildren, NamePlateDesign design, PrintLayout layout, int pageIndex)
    {
        foreach (var placement in layout.Placements.Where(item => item.PageIndex == pageIndex))
        {
            var plate = CreateNamePlateElement(design, placement.Entry, showOutputGuide: true);
            Canvas.SetLeft(plate, placement.LeftPixels);
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

        var backgroundImage = ImageHelper.CreateImageSource(entry.BackgroundImagePath);
        if (backgroundImage is not null)
        {
            root.Children.Add(new Image
            {
                Source = backgroundImage,
                Width = plateWidth,
                Height = plateHeight,
                Stretch = Stretch.UniformToFill,
                IsHitTestVisible = false
            });
        }

        root.Children.Add(new Border
        {
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(Math.Max(0, design.BorderThickness)),
            Background = Brushes.Transparent
        });

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
            root.Children.Add(new Rectangle
            {
                Stroke = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            });
        }

        return root;
    }

    private static void AddDecorationLines(Canvas canvas, double plateWidth, double plateHeight, Brush stroke)
    {
        var startX = plateWidth * 0.08;
        var endX = plateWidth * 0.92;

        foreach (var y in new[] { plateHeight * 0.24, plateHeight * 0.76 })
        {
            canvas.Children.Add(new Line
            {
                X1 = startX,
                X2 = endX,
                Y1 = y,
                Y2 = y,
                Stroke = stroke,
                StrokeThickness = 1.5,
                Opacity = 0.75
            });
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
            Stretch = Stretch.Uniform,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(image, UnitConverter.MillimetersToPixels(Math.Max(0, design.OverlayImageX)));
        Canvas.SetTop(image, UnitConverter.MillimetersToPixels(Math.Max(0, design.OverlayImageY)));
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
