using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using NamePlateStudio.Models;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NamePlateStudio.Services;

public sealed class FileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string? Save(NamePlateDesign design)
    {
        var dialog = new SaveFileDialog
        {
            Title = "명패 양식 저장",
            FileName = "nameplate-template.json",
            DefaultExt = ".json",
            Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(design, JsonOptions);
        File.WriteAllText(dialog.FileName, json);
        return dialog.FileName;
    }

    public (NamePlateDesign? Design, string? Path) Load()
    {
        var dialog = new OpenFileDialog
        {
            Title = "명패 양식 불러오기",
            DefaultExt = ".json",
            Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return (null, null);
        }

        var json = File.ReadAllText(dialog.FileName);
        var design = JsonSerializer.Deserialize<NamePlateDesign>(json, JsonOptions);
        return (design, dialog.FileName);
    }
}
