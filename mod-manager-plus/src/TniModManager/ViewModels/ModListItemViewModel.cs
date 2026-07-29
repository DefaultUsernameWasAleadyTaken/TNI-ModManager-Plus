using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TniModManager.Core.Models;
using TniModManager.Localization;

namespace TniModManager.ViewModels;

public partial class ModListItemViewModel : ObservableObject
{
    public ModInfo Mod { get; }

    public ModListItemViewModel(ModInfo mod)
    {
        Mod = mod;
        RefreshBrush();
        RefreshLocalizedLabels();
    }

    public string DisplayName => Mod.Name;
    [ObservableProperty] private string _sourceLabel = "";
    [ObservableProperty] private IBrush _borderBrush = Brushes.Gray;
    public bool ShowUpdateBadge => Mod.HasUpdate;

    public void RefreshBrush() => BorderBrush = ThemeBrushResolver.Get(Mod.Source switch
    {
        ModSource.Downloaded => "SourceDownloadedBrush",
        ModSource.Manual => "SourceManualBrush",
        _ => "SourceAvailableBrush"
    });

    public void RefreshLocalizedLabels() => SourceLabel = UiStrings.FormatModSource(Mod.Source);
}
