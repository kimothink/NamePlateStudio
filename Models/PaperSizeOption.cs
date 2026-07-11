namespace NamePlateStudio.Models;

public sealed record PaperSizeOption(string Name, double WidthMm, double HeightMm, bool IsCustom = false)
{
    public override string ToString()
    {
        return Name;
    }
}
