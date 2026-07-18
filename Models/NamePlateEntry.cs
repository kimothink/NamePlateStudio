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

    [ObservableProperty]
    private string backContent = string.Empty;

    [ObservableProperty]
    private string backImagePath = string.Empty;

    [ObservableProperty]
    private double backImageX = 8;

    [ObservableProperty]
    private double backImageY = 8;

    [ObservableProperty]
    private double backImageWidthMm = 40;

    [ObservableProperty]
    private double backImageHeightMm = 25;

    [ObservableProperty]
    private double backImageRotation;

    [ObservableProperty]
    private int backTableRows;

    [ObservableProperty]
    private int backTableColumns;

    [ObservableProperty]
    private List<string> backTableCells = [];

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

    public NamePlateEntry(string nameText, string titleText, string companyText, string backgroundImagePath, string overlayImagePath, string backContent, string backImagePath = "", double backImageWidthMm = 40, double backImageHeightMm = 25, int backTableRows = 0, int backTableColumns = 0, List<string>? backTableCells = null, double backImageX = 8, double backImageY = 8, double backImageRotation = 0)
        : this(nameText, titleText, companyText, backgroundImagePath, overlayImagePath)
    {
        this.backContent = backContent;
        this.backImagePath = backImagePath;
        this.backImageX = backImageX;
        this.backImageY = backImageY;
        this.backImageWidthMm = backImageWidthMm;
        this.backImageHeightMm = backImageHeightMm;
        this.backImageRotation = backImageRotation;
        this.backTableRows = backTableRows;
        this.backTableColumns = backTableColumns;
        this.backTableCells = backTableCells?.ToList() ?? [];
    }
}
