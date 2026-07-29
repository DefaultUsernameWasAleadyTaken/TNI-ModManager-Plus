using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TniModManager.Core.Models;

namespace TniModManager.ViewModels;

public partial class ModListItemViewModel : ObservableObject
{
    public ModInfo Mod { get; }

    public ModListItemViewModel(ModInfo mod)
    {
        Mod = mod;
        RefreshBrush();
    }

    public string DisplayName => Mod.Name;
    public string SourceLabel => Mod.Source.ToString();
    [ObservableProperty] private IBrush _borderBrush = Brushes.Gray;
    public bool ShowUpdateBadge => Mod.HasUpdate;

    public void RefreshBrush() => BorderBrush = ThemeBrushResolver.Get(Mod.Source switch
    {
        ModSource.Downloaded => "SourceDownloadedBrush",
        ModSource.Manual => "SourceManualBrush",
        _ => "SourceAvailableBrush"
    });
}
