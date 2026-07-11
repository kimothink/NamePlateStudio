using CommunityToolkit.Mvvm.ComponentModel;

namespace NamePlateStudio.Models;

public partial class NamePlateEntry : ObservableObject
{
    [ObservableProperty]
    private string nameText = string.Empty;

    [ObservableProperty]
    private string titleText = string.Empty;

    [ObservableProperty]
    private string companyText = string.Empty;

    [ObservableProperty]
    private string backgroundImagePath = string.Empty;

    [ObservableProperty]
    private string overlayImagePath = string.Empty;

    public NamePlateEntry()
    {
    }

    public NamePlateEntry(string nameText, string titleText, string companyText)
    {
        this.nameText = nameText;
        this.titleText = titleText;
        this.companyText = companyText;
    }

    public NamePlateEntry(string nameText, string titleText, string companyText, string backgroundImagePath, string overlayImagePath)
    {
        this.nameText = nameText;
        this.titleText = titleText;
        this.companyText = companyText;
        this.backgroundImagePath = backgroundImagePath;
        this.overlayImagePath = overlayImagePath;
    }
}
