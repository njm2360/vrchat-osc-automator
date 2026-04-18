using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface IDialogService
{
    void ShowExportDialog(string exportData);

    string? ShowImportDialog();

    bool ConfirmOverwrite();

    void ShowError(string message);

    IReadOnlyList<OscTarget>? ShowSendTargetsWindow(IEnumerable<OscTarget> currentTargets);

    HotkeySettings? ShowHotkeySettingsWindow(HotkeySettings currentSettings);

    KeyRepeatSettings? ShowKeyRepeatSettingsWindow(KeyRepeatSettings currentSettings);

    bool ConfirmDeleteProfile(string profileName);

    void ShowAboutWindow();
}
