using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TniModManager.Views;

public partial class ConfirmDialogWindow : Window
{
    public ConfirmDialogWindow()
    {
        InitializeComponent();
    }

    public static Task<bool> ShowAsync(
        Window owner,
        string title,
        string message,
        string confirmLabel,
        string cancelLabel,
        bool isDanger = true)
    {
        var dialog = new ConfirmDialogWindow();
        dialog.Title = title;
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.ConfirmButton.Content = confirmLabel;
        dialog.CancelButton.Content = cancelLabel;
        if (!isDanger)
        {
            dialog.ConfirmButton.Classes.Remove("danger");
            dialog.ConfirmButton.Classes.Add("primary");
        }

        return dialog.ShowDialog<bool>(owner);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
