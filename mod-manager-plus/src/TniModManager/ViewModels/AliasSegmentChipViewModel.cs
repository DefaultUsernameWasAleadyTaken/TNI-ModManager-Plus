using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TniModManager.ViewModels;

public partial class AliasSegmentChipViewModel : ObservableObject
{
    public AliasSegmentChipViewModel(
        int index,
        string title,
        int caretStart,
        IRelayCommand<int> selectCommand)
    {
        Index = index;
        Title = title;
        CaretStart = caretStart;
        SelectCommand = selectCommand;
    }

    public int Index { get; }
    public string Title { get; }
    public int CaretStart { get; }
    public IRelayCommand<int> SelectCommand { get; }
    [ObservableProperty] private bool _isActive;
}
