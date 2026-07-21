using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using NamePlateStudio.Models;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NamePlateStudio.Services;

public sealed class FileService
{
    public const string TemplateExtension = ".nps";

    private const string DesignEntryName = "design.json";
    private const long MaxEmbeddedImageBytes = 100L * 1024 * 1024;
    private const long MaxTotalImageBytes = 500L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string? Save(NamePlateDesign design)
    {
        var dialog = new SaveFileDialog
        {
            Title = "명패 양식 저장",
            FileName = $"nameplate-template{TemplateExtension}",
            DefaultExt = TemplateExtension,
            AddExtension = true,
            Filter = "NamePlateStudio 양식 (*.nps)|*.nps"
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        SaveToPath(design, dialog.FileName);
        return dialog.FileName;
    }

    public void SaveToPath(NamePlateDesign design, string path)
    {
        var packagedDesign = CloneDesign(design);
        var packagedImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var imageNumber = 0;

        foreach (var entry in packagedDesign.Entries ?? [])
        {
            entry.BackgroundImagePath = PackageImagePath(entry.BackgroundImagePath, packagedImages, ref imageNumber);
            entry.OverlayImagePath = PackageImagePath(entry.OverlayImagePath, packagedImages, ref imageNumber);
            entry.BackImagePath = PackageImagePath(entry.BackImagePath, packagedImages, ref imageNumber);
        }

        var targetPath = Path.GetFullPath(path);
        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            WritePackage(temporaryPath, packagedDesign, packagedImages);
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public (NamePlateDesign? Design, string? Path) Load()
    {
        var dialog = new OpenFileDialog
        {
            Title = "명패 양식 불러오기",
            DefaultExt = TemplateExtension,
            Filter = "NamePlateStudio 양식 (*.nps)|*.nps|기존 JSON 양식 (*.json)|*.json|모든 파일 (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return (null, null);
        }

        return (LoadFromPath(dialog.FileName), dialog.FileName);
    }

    public NamePlateDesign? LoadFromPath(string path)
    {
        if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            var legacyJson = File.ReadAllText(path);
            return JsonSerializer.Deserialize<NamePlateDesign>(legacyJson, JsonOptions);
        }

        using var archive = ZipFile.OpenRead(path);
        var designEntry = archive.GetEntry(DesignEntryName)
            ?? throw new InvalidDataException("올바른 NamePlateStudio 양식 파일이 아닙니다.");

        NamePlateDesign? design;
        using (var designStream = designEntry.Open())
        {
            design = JsonSerializer.Deserialize<NamePlateDesign>(designStream, JsonOptions);
        }

        if (design is null)
        {
            throw new InvalidDataException("양식 정보를 읽을 수 없습니다.");
        }

        var cacheDirectory = CreatePackageCacheDirectory(path);
        var extractedImages = ExtractPackagedImages(archive, cacheDirectory);
        foreach (var entry in design.Entries ?? [])
        {
            entry.BackgroundImagePath = ResolvePackagedImagePath(entry.BackgroundImagePath, extractedImages);
            entry.OverlayImagePath = ResolvePackagedImagePath(entry.OverlayImagePath, extractedImages);
            entry.BackImagePath = ResolvePackagedImagePath(entry.BackImagePath, extractedImages);
        }

        return design;
    }

    private static NamePlateDesign CloneDesign(NamePlateDesign design)
    {
        var json = JsonSerializer.Serialize(design, JsonOptions);
        return JsonSerializer.Deserialize<NamePlateDesign>(json, JsonOptions)
            ?? throw new InvalidDataException("양식 정보를 저장할 수 없습니다.");
    }

    private static void WritePackage(
        string path,
        NamePlateDesign design,
        IReadOnlyDictionary<string, string> packagedImages)
    {
        using var packageStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Create);
        var designEntry = archive.CreateEntry(DesignEntryName, CompressionLevel.Optimal);
        using (var designStream = designEntry.Open())
        {
            JsonSerializer.Serialize(designStream, design, JsonOptions);
        }

        foreach (var (sourcePath, archivePath) in packagedImages)
        {
            var imageEntry = archive.CreateEntry(archivePath, CompressionLevel.Optimal);
            using var source = File.OpenRead(sourcePath);
            using var destination = imageEntry.Open();
            source.CopyTo(destination);
        }
    }

    private static string PackageImagePath(
        string? imagePath,
        Dictionary<string, string> packagedImages,
        ref int imageNumber)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return string.Empty;
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("양식에 사용된 이미지 파일을 찾을 수 없습니다.", imagePath);
        }

        var sourcePath = Path.GetFullPath(imagePath);
        if (packagedImages.TryGetValue(sourcePath, out var existingArchivePath))
        {
            return existingArchivePath;
        }

        imageNumber++;
        var extension = GetSafeImageExtension(sourcePath);
        var archivePath = $"assets/image-{imageNumber:D4}{extension}";
        packagedImages.Add(sourcePath, archivePath);
        return archivePath;
    }

    private static string GetSafeImageExtension(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension.Length is >= 2 and <= 10 && extension[1..].All(char.IsLetterOrDigit)
            ? extension
            : ".bin";
    }

    private static string CreatePackageCacheDirectory(string packagePath)
    {
        using var packageStream = File.OpenRead(packagePath);
        var packageHash = Convert.ToHexString(SHA256.HashData(packageStream));
        var cacheDirectory = Path.Combine(Path.GetTempPath(), "NamePlateStudio", "ImportedAssets", packageHash);
        Directory.CreateDirectory(cacheDirectory);
        return cacheDirectory;
    }

    private static Dictionary<string, string> ExtractPackagedImages(ZipArchive archive, string cacheDirectory)
    {
        var extractedImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        long totalImageBytes = 0;

        foreach (var entry in archive.Entries.Where(item =>
                     item.FullName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) &&
                     !string.IsNullOrWhiteSpace(item.Name)))
        {
            if (entry.Length > MaxEmbeddedImageBytes || totalImageBytes + entry.Length > MaxTotalImageBytes)
            {
                throw new InvalidDataException("양식에 포함된 이미지 용량이 너무 큽니다.");
            }

            totalImageBytes += entry.Length;
            var outputPath = Path.Combine(cacheDirectory, entry.Name);
            using var source = entry.Open();
            using var destination = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            source.CopyTo(destination);
            extractedImages[NormalizeArchivePath(entry.FullName)] = outputPath;
        }

        return extractedImages;
    }

    private static string ResolvePackagedImagePath(
        string? imagePath,
        IReadOnlyDictionary<string, string> extractedImages)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return string.Empty;
        }

        return extractedImages.TryGetValue(NormalizeArchivePath(imagePath), out var extractedPath)
            ? extractedPath
            : string.Empty;
    }

    private static string NormalizeArchivePath(string path) => path.Replace('\\', '/');
}
