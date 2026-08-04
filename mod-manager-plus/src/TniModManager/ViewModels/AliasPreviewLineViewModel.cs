using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TniModManager.ViewModels;

public partial class AliasPreviewLineViewModel : ObservableObject
{
    public AliasPreviewLineViewModel(
        int index,
        string prefix,
        string body,
        int caretStart,
        IRelayCommand<int> selectCommand,
        string? fullText = null)
    {
        Index = index;
        Prefix = prefix;
        Body = body;
        FullText = fullText ?? body;
        CaretStart = caretStart;
        SelectCommand = selectCommand;
    }

    public int Index { get; }
    public string Prefix { get; }
    public string Body { get; }
    public string FullText { get; }
    public int CaretStart { get; }
    public IRelayCommand<int> SelectCommand { get; }
    [ObservableProperty] private bool _isActive;
}
