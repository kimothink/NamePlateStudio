using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NamePlateStudio.Models;

public partial class NamePlateGroup : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    public ObservableCollection<NamePlateEntry> Entries { get; } = [];

    public NamePlateGroup(string name)
    {
        this.name = name;
    }
}
