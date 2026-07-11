using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NamePlateStudio.Services;

public sealed class ImageFileService
{
    public string? PickImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "이미지 선택",
            Filter = "이미지 파일 (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|모든 파일 (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
