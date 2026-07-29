using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TniModManager.Core.Aliases;

namespace TniModManager.ViewModels;

public partial class AliasListItemViewModel : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _command;

    public AliasListItemViewModel(string name, string command)
    {
        _name = name;
        _command = command;
        RefreshBrush();
    }

    public AliasKind Kind => AliasAnalyzer.Analyze(Command);
    public string KindLabel => Kind.ToString();
    [ObservableProperty] private IBrush _kindBrush = Brushes.Gray;

    public void RefreshBrush() => KindBrush = ThemeBrushResolver.GetAlias(Kind);
}
