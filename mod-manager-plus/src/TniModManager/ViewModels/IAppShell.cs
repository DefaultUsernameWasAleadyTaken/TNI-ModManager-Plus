namespace TniModManager.ViewModels;

public interface IAppShell
{
    bool IsBusy { get; }
    void SetStatus(string text, bool isError = false);
    /// <summary>Статус + краткий toast (успех / info).</summary>
    void Notify(string text);
    bool TryEnterBusy();
    void ExitBusy();
    void BeginProgress(string statusText);
    void ReportProgress(double percent, string statusText);
    void EndProgress();
    Task<bool> ConfirmAsync(string title, string message, string? confirmLabel = null, bool isDanger = true);
}
