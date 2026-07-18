using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using NamePlateStudio.Helpers;
using NamePlateStudio.Models;
using NamePlateStudio.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace NamePlateStudio.Views;

public partial class MainWindow : Window
{
    private int previewWheelDelta;
    private PreviewImageDragState? previewImageDragState;
    private PreviewImageResizeState? previewImageResizeState;
    private PreviewImageRotationState? previewImageRotationState;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "종료하시겠습니까?",
            "NamePlateStudio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void PaperPreview_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && IsInsidePreviewImageEditor(source))
        {
            return;
        }

        if (DataContext is not MainViewModel viewModel || viewModel.PreviewPageCount <= 1)
        {
            return;
        }

        if (previewWheelDelta != 0 && Math.Sign(previewWheelDelta) != Math.Sign(e.Delta))
        {
            previewWheelDelta = 0;
        }

        previewWheelDelta += e.Delta;
        if (Math.Abs(previewWheelDelta) < Mouse.MouseWheelDeltaForOneLine)
        {
            e.Handled = true;
            return;
        }

        if (previewWheelDelta < 0)
        {
            viewModel.NextPreviewPageCommand.Execute(null);
        }
        else
        {
            viewModel.PreviousPreviewPageCommand.Execute(null);
        }

        previewWheelDelta = 0;
        e.Handled = true;
    }

    private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not NamePlatePlacement placement ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var parentCanvas = FindVisualParent<Canvas>(element);
        if (parentCanvas is null)
        {
            return;
        }

        viewModel.SelectedEntry = placement.Entry;
        var isBackImage = IsBackImageElement(element);
        var startPoint = e.GetPosition(parentCanvas);
        previewImageDragState = new PreviewImageDragState(
            element,
            parentCanvas,
            placement,
            isBackImage,
            startPoint,
            isBackImage ? placement.Entry.BackImageX : viewModel.OverlayImageX,
            isBackImage ? placement.Entry.BackImageY : viewModel.OverlayImageY);

        element.CaptureMouse();
        e.Handled = true;
    }

    private void PreviewImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (previewImageDragState is null ||
            sender is not FrameworkElement element ||
            !ReferenceEquals(element, previewImageDragState.Element) ||
            e.LeftButton != MouseButtonState.Pressed ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var currentPoint = e.GetPosition(previewImageDragState.ParentCanvas);
        var deltaXMm = UnitConverter.PixelsToMillimeters(currentPoint.X - previewImageDragState.StartPoint.X);
        var deltaYMm = UnitConverter.PixelsToMillimeters(currentPoint.Y - previewImageDragState.StartPoint.Y);

        MovePreviewImage(
            viewModel,
            previewImageDragState.Placement.Entry,
            previewImageDragState.IsBackImage,
            previewImageDragState.StartXMm + deltaXMm,
            previewImageDragState.StartYMm + deltaYMm);

        e.Handled = true;
    }

    private void PreviewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.ReleaseMouseCapture();
        }

        previewImageDragState = null;
        e.Handled = true;
    }

    private void PreviewImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not NamePlatePlacement placement ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedEntry = placement.Entry;
        ScalePreviewImage(viewModel, placement.Entry, IsBackImageElement(element), e.Delta > 0 ? 1.1 : 0.9);
        e.Handled = true;
    }

    private void PreviewImageContextMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem ||
            menuItem.Parent is not ContextMenu contextMenu ||
            contextMenu.PlacementTarget is not FrameworkElement element ||
            element.DataContext is not NamePlatePlacement placement ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedEntry = placement.Entry;
        var isBackImage = IsBackImageElement(element);
        switch (menuItem.Tag?.ToString())
        {
            case "Fill":
                FillPreviewImage(viewModel, placement.Entry, isBackImage);
                break;
            case "RotateLeft":
                RotatePreviewImage(viewModel, placement.Entry, isBackImage, -90);
                break;
            case "RotateRight":
                RotatePreviewImage(viewModel, placement.Entry, isBackImage, 90);
                break;
        }

        e.Handled = true;
    }

    private void PreviewImageResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is not Thumb thumb ||
            FindImageEditor(thumb) is not { } editor ||
            editor.DataContext is not NamePlatePlacement placement ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var parentCanvas = FindVisualParent<Canvas>(editor);
        if (parentCanvas is null)
        {
            return;
        }

        viewModel.SelectedEntry = placement.Entry;
        var isBackImage = IsBackImageElement(editor);
        previewImageResizeState = new PreviewImageResizeState(
            placement.Entry,
            isBackImage,
            parentCanvas,
            Mouse.GetPosition(parentCanvas),
            GetResizeCorner(thumb),
            isBackImage ? placement.Entry.BackImageX : viewModel.OverlayImageX,
            isBackImage ? placement.Entry.BackImageY : viewModel.OverlayImageY,
            isBackImage ? placement.Entry.BackImageWidthMm : viewModel.OverlayImageWidth,
            isBackImage ? placement.Entry.BackImageHeightMm : viewModel.OverlayImageHeight,
            isBackImage ? placement.Entry.BackImageRotation : viewModel.OverlayImageRotation);
    }

    private void PreviewImageResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (previewImageResizeState is not { } state || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var pointer = Mouse.GetPosition(state.ParentCanvas);
        var screenDeltaX = pointer.X - state.StartPointer.X;
        var screenDeltaY = pointer.Y - state.StartPointer.Y;
        var radians = state.Rotation * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var localDeltaX = screenDeltaX * cos + screenDeltaY * sin;
        var localDeltaY = -screenDeltaX * sin + screenDeltaY * cos;

        ResizePreviewImage(
            viewModel,
            state.Entry,
            state.IsBackImage,
            state.Corner,
            UnitConverter.PixelsToMillimeters(localDeltaX),
            UnitConverter.PixelsToMillimeters(localDeltaY),
            state.StartX,
            state.StartY,
            state.StartWidth,
            state.StartHeight,
            state.Rotation);
    }

    private void PreviewImageResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        previewImageResizeState = null;
    }

    private void PreviewImageRotateThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is not Thumb thumb ||
            FindImageEditor(thumb) is not { } editor ||
            editor.DataContext is not NamePlatePlacement placement ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var parentCanvas = FindVisualParent<Canvas>(editor);
        if (parentCanvas is null)
        {
            return;
        }

        viewModel.SelectedEntry = placement.Entry;
        var center = editor.TranslatePoint(
            new System.Windows.Point(editor.ActualWidth / 2, editor.ActualHeight / 2),
            parentCanvas);
        var pointer = Mouse.GetPosition(parentCanvas);
        var isBackImage = IsBackImageElement(editor);
        previewImageRotationState = new PreviewImageRotationState(
            placement.Entry,
            isBackImage,
            parentCanvas,
            center,
            GetPointerAngle(center, pointer),
            isBackImage ? placement.Entry.BackImageRotation : viewModel.OverlayImageRotation);
    }

    private void PreviewImageRotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (previewImageRotationState is not { } state || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var pointerAngle = GetPointerAngle(state.Center, Mouse.GetPosition(state.ParentCanvas));
        var delta = NormalizeSignedDegrees(pointerAngle - state.StartPointerAngle);
        var rotation = state.StartRotation + delta;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            rotation = Math.Round(rotation / 15.0) * 15.0;
        }

        SetPreviewImageRotation(viewModel, state.Entry, state.IsBackImage, rotation);
    }

    private void PreviewImageRotateThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        previewImageRotationState = null;
    }

    private static void MovePreviewImage(MainViewModel viewModel, NamePlateEntry entry, bool isBackImage, double targetXMm, double targetYMm)
    {
        var width = isBackImage ? entry.BackImageWidthMm : viewModel.OverlayImageWidth;
        var height = isBackImage ? entry.BackImageHeightMm : viewModel.OverlayImageHeight;
        var rotation = isBackImage ? entry.BackImageRotation : viewModel.OverlayImageRotation;
        var position = ConstrainImagePosition(viewModel, targetXMm, targetYMm, width, height, rotation);

        if (isBackImage)
        {
            entry.BackImageX = position.X;
            entry.BackImageY = position.Y;
        }
        else
        {
            viewModel.OverlayImageX = position.X;
            viewModel.OverlayImageY = position.Y;
        }
    }

    private static void ResizePreviewImage(
        MainViewModel viewModel,
        NamePlateEntry entry,
        bool isBackImage,
        ResizeCorner corner,
        double deltaWidthMm,
        double deltaHeightMm,
        double x,
        double y,
        double width,
        double height,
        double rotation)
    {
        const double MinimumImageSizeMm = 5;

        if (viewModel.LockImageAspectRatio && width > 0 && height > 0)
        {
            ResizePreviewImageWithLockedRatio(
                viewModel, entry, isBackImage, corner, deltaWidthMm, deltaHeightMm,
                x, y, width, height, rotation, MinimumImageSizeMm);
            return;
        }

        var right = x + width;
        var bottom = y + height;

        if (corner is ResizeCorner.TopLeft or ResizeCorner.BottomLeft)
        {
            var newX = Math.Min(x + deltaWidthMm, right - MinimumImageSizeMm);
            width = right - newX;
            x = newX;
        }
        else
        {
            var newRight = Math.Max(right + deltaWidthMm, x + MinimumImageSizeMm);
            width = newRight - x;
        }

        if (corner is ResizeCorner.TopLeft or ResizeCorner.TopRight)
        {
            var newY = Math.Min(y + deltaHeightMm, bottom - MinimumImageSizeMm);
            height = bottom - newY;
            y = newY;
        }
        else
        {
            var newBottom = Math.Max(bottom + deltaHeightMm, y + MinimumImageSizeMm);
            height = newBottom - y;
        }

        ApplyPreviewImagePlacement(viewModel, entry, isBackImage, x, y, width, height, rotation);
    }

    private static void ResizePreviewImageWithLockedRatio(
        MainViewModel viewModel,
        NamePlateEntry entry,
        bool isBackImage,
        ResizeCorner corner,
        double deltaWidthMm,
        double deltaHeightMm,
        double x,
        double y,
        double width,
        double height,
        double rotation,
        double minimumImageSizeMm)
    {
        var movesLeft = corner is ResizeCorner.TopLeft or ResizeCorner.BottomLeft;
        var movesTop = corner is ResizeCorner.TopLeft or ResizeCorner.TopRight;
        var widthChange = movesLeft ? -deltaWidthMm : deltaWidthMm;
        var heightChange = movesTop ? -deltaHeightMm : deltaHeightMm;
        var relativeWidthChange = widthChange / width;
        var relativeHeightChange = heightChange / height;
        var requestedScale = 1 + (Math.Abs(relativeWidthChange) >= Math.Abs(relativeHeightChange)
            ? relativeWidthChange
            : relativeHeightChange);

        var fixedX = movesLeft ? x + width : x;
        var fixedY = movesTop ? y + height : y;
        var minimumScale = Math.Max(minimumImageSizeMm / width, minimumImageSizeMm / height);
        var rotatedBounds = GetRotatedImageBounds(width, height, rotation);
        var maximumScale = Math.Min(
            Math.Max(1, viewModel.WidthMm) / rotatedBounds.Width,
            Math.Max(1, viewModel.HeightMm) / rotatedBounds.Height);
        var scale = Clamp(requestedScale, Math.Min(minimumScale, maximumScale), Math.Max(minimumScale, maximumScale));
        var newWidth = width * scale;
        var newHeight = height * scale;
        var newX = movesLeft ? fixedX - newWidth : fixedX;
        var newY = movesTop ? fixedY - newHeight : fixedY;

        ApplyPreviewImagePlacement(viewModel, entry, isBackImage, newX, newY, newWidth, newHeight, rotation);
    }

    private static void ScalePreviewImage(MainViewModel viewModel, NamePlateEntry entry, bool isBackImage, double factor)
    {
        const double MinimumImageSizeMm = 5;
        var x = isBackImage ? entry.BackImageX : viewModel.OverlayImageX;
        var y = isBackImage ? entry.BackImageY : viewModel.OverlayImageY;
        var width = isBackImage ? entry.BackImageWidthMm : viewModel.OverlayImageWidth;
        var height = isBackImage ? entry.BackImageHeightMm : viewModel.OverlayImageHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var centerX = x + width / 2;
        var centerY = y + height / 2;
        var minimumScale = Math.Max(MinimumImageSizeMm / width, MinimumImageSizeMm / height);
        var rotation = isBackImage ? entry.BackImageRotation : viewModel.OverlayImageRotation;
        var rotatedBounds = GetRotatedImageBounds(width, height, rotation);
        var maximumScale = Math.Min(
            Math.Max(1, viewModel.WidthMm) / rotatedBounds.Width,
            Math.Max(1, viewModel.HeightMm) / rotatedBounds.Height);
        var scale = Clamp(factor, Math.Min(minimumScale, maximumScale), Math.Max(minimumScale, maximumScale));
        var newWidth = width * scale;
        var newHeight = height * scale;
        var newX = centerX - newWidth / 2;
        var newY = centerY - newHeight / 2;
        ApplyPreviewImagePlacement(viewModel, entry, isBackImage, newX, newY, newWidth, newHeight, rotation);
    }

    private static void ApplyPreviewImagePlacement(
        MainViewModel viewModel,
        NamePlateEntry entry,
        bool isBackImage,
        double x,
        double y,
        double width,
        double height,
        double rotation)
    {
        var position = ConstrainImagePosition(viewModel, x, y, width, height, rotation);
        if (isBackImage)
        {
            entry.BackImageX = position.X;
            entry.BackImageY = position.Y;
            entry.BackImageWidthMm = width;
            entry.BackImageHeightMm = height;
        }
        else
        {
            viewModel.OverlayImageX = position.X;
            viewModel.OverlayImageY = position.Y;
            viewModel.OverlayImageWidth = width;
            viewModel.OverlayImageHeight = height;
        }
    }

    private static (double X, double Y) ConstrainImagePosition(
        MainViewModel viewModel,
        double x,
        double y,
        double width,
        double height,
        double rotation)
    {
        var bounds = GetRotatedImageBounds(width, height, rotation);
        var plateWidth = Math.Max(1, viewModel.WidthMm);
        var plateHeight = Math.Max(1, viewModel.HeightMm);

        var constrainedX = bounds.Width <= plateWidth
            ? Clamp(x, -bounds.OffsetX, plateWidth - bounds.Width - bounds.OffsetX)
            : (plateWidth - width) / 2;
        var constrainedY = bounds.Height <= plateHeight
            ? Clamp(y, -bounds.OffsetY, plateHeight - bounds.Height - bounds.OffsetY)
            : (plateHeight - height) / 2;

        return (constrainedX, constrainedY);
    }

    private static (double Width, double Height, double OffsetX, double OffsetY) GetRotatedImageBounds(
        double width,
        double height,
        double rotation)
    {
        var radians = rotation * Math.PI / 180.0;
        var absoluteCos = Math.Abs(Math.Cos(radians));
        var absoluteSin = Math.Abs(Math.Sin(radians));
        var boundsWidth = width * absoluteCos + height * absoluteSin;
        var boundsHeight = width * absoluteSin + height * absoluteCos;
        return (boundsWidth, boundsHeight, (width - boundsWidth) / 2, (height - boundsHeight) / 2);
    }

    private static void FillPreviewImage(MainViewModel viewModel, NamePlateEntry entry, bool isBackImage)
    {
        if (isBackImage)
        {
            entry.BackImageX = 0;
            entry.BackImageY = 0;
            entry.BackImageWidthMm = Math.Max(1, viewModel.WidthMm);
            entry.BackImageHeightMm = Math.Max(1, viewModel.HeightMm);
        }
        else
        {
            viewModel.OverlayImageX = 0;
            viewModel.OverlayImageY = 0;
            viewModel.OverlayImageWidth = Math.Max(1, viewModel.WidthMm);
            viewModel.OverlayImageHeight = Math.Max(1, viewModel.HeightMm);
        }
    }

    private static void RotatePreviewImage(MainViewModel viewModel, NamePlateEntry entry, bool isBackImage, double deltaDegrees)
    {
        if (isBackImage)
        {
            entry.BackImageRotation = NormalizeDegrees(entry.BackImageRotation + deltaDegrees);
        }
        else
        {
            viewModel.OverlayImageRotation = NormalizeDegrees(viewModel.OverlayImageRotation + deltaDegrees);
        }
    }

    private static void SetPreviewImageRotation(MainViewModel viewModel, NamePlateEntry entry, bool isBackImage, double degrees)
    {
        if (isBackImage)
        {
            entry.BackImageRotation = NormalizeDegrees(degrees);
        }
        else
        {
            viewModel.OverlayImageRotation = NormalizeDegrees(degrees);
        }
    }

    private static double GetPointerAngle(System.Windows.Point center, System.Windows.Point pointer)
        => Math.Atan2(pointer.Y - center.Y, pointer.X - center.X) * 180.0 / Math.PI;

    private static double NormalizeSignedDegrees(double degrees)
    {
        var normalized = (degrees + 180) % 360;
        if (normalized < 0)
        {
            normalized += 360;
        }
        return normalized - 180;
    }

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static bool IsBackImageElement(FrameworkElement element)
        => element.Tag?.ToString() == "BackImage";

    private static FrameworkElement? FindImageEditor(DependencyObject child)
    {
        var current = VisualTreeHelper.GetParent(child);
        while (current is not null)
        {
            if (current is FrameworkElement { Tag: "FrontOverlay" or "BackImage" } editor)
            {
                return editor;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static ResizeCorner GetResizeCorner(FrameworkElement element)
    {
        return element.Tag?.ToString() switch
        {
            "TopLeft" => ResizeCorner.TopLeft,
            "TopRight" => ResizeCorner.TopRight,
            "BottomLeft" => ResizeCorner.BottomLeft,
            _ => ResizeCorner.BottomRight
        };
    }

    private static bool IsInsidePreviewImageEditor(DependencyObject source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is FrameworkElement { Tag: "FrontOverlay" or "BackImage" })
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static double Clamp(double value, double min, double max)
        => Math.Min(Math.Max(value, min), max);

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(child);
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private sealed record PreviewImageDragState(
        FrameworkElement Element,
        Canvas ParentCanvas,
        NamePlatePlacement Placement,
        bool IsBackImage,
        System.Windows.Point StartPoint,
        double StartXMm,
        double StartYMm);

    private sealed record PreviewImageResizeState(
        NamePlateEntry Entry,
        bool IsBackImage,
        Canvas ParentCanvas,
        System.Windows.Point StartPointer,
        ResizeCorner Corner,
        double StartX,
        double StartY,
        double StartWidth,
        double StartHeight,
        double Rotation);

    private sealed record PreviewImageRotationState(
        NamePlateEntry Entry,
        bool IsBackImage,
        Canvas ParentCanvas,
        System.Windows.Point Center,
        double StartPointerAngle,
        double StartRotation);

    private enum ResizeCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
