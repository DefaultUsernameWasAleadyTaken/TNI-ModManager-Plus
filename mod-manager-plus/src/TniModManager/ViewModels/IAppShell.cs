namespace TniModManager.ViewModels;

public interface IAppShell
{
    void SetStatus(string text);
    bool TryEnterBusy();
    void ExitBusy();
    void BeginProgress(string statusText);
    void ReportProgress(double percent, string statusText);
    void EndProgress();
}
