using NamePlateStudio.Helpers;
using DrawingColor = System.Drawing.Color;
using FormsColorDialog = System.Windows.Forms.ColorDialog;
using FormsDialogResult = System.Windows.Forms.DialogResult;

namespace NamePlateStudio.Services;

public sealed class ColorDialogService
{
    public string? PickColor(string currentColor)
    {
        using var dialog = new FormsColorDialog
        {
            AnyColor = true,
            FullOpen = true
        };

        if (ColorHelper.TryParseColor(currentColor, out var parsedColor))
        {
            dialog.Color = DrawingColor.FromArgb(parsedColor.R, parsedColor.G, parsedColor.B);
        }

        return dialog.ShowDialog() == FormsDialogResult.OK
            ? $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"
            : null;
    }
}
