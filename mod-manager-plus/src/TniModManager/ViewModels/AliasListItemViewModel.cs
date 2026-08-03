using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TniModManager.Core.Aliases;
using TniModManager.Localization;

namespace TniModManager.ViewModels;

public partial class AliasListItemViewModel : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _command;
    [ObservableProperty] private string _kindLabel = "";
    [ObservableProperty] private string _commandPreview = "";
    [ObservableProperty] private IBrush _kindBrush = Brushes.Gray;

    public AliasListItemViewModel(string name, string command)
    {
        _name = name;
        _command = command;
        RefreshBrush();
        RefreshLocalizedLabels();
        RefreshCommandPreview();
    }

    public AliasKind Kind => AliasAnalyzer.Analyze(Command).Kind;

    partial void OnCommandChanged(string value)
    {
        RefreshBrush();
        RefreshLocalizedLabels();
        RefreshCommandPreview();
    }

    public void RefreshBrush() => KindBrush = ThemeBrushResolver.GetAlias(Kind);

    public void RefreshLocalizedLabels() => KindLabel = UiStrings.FormatAliasKind(Kind);

    public void RefreshCommandPreview()
    {
        var text = Command.Replace('\r', ' ').Replace('\n', ' ').Trim();
        CommandPreview = string.IsNullOrEmpty(text) ? UiStrings.EmptyPreview : text;
    }
}
