using System.Windows;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using VrcOscAutomator.Views;

namespace VrcOscAutomator.Services;

public sealed class DialogService : IDialogService
{
    public void ShowExportDialog(string exportData)
    {
        ExportDialog dialog = new(exportData) { Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
    }

    public string? ShowImportDialog()
    {
        ImportDialog dialog = new() { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog() == true ? dialog.InputText : null;
    }

    public bool ConfirmOverwrite()
    {
        MessageBoxResult result = MessageBox.Show(
            Application.Current.MainWindow,
            "既存のスロットが上書きされます。続行しますか？",
            "上書き確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    public bool ConfirmDeleteProfile(string profileName)
    {
        MessageBoxResult result = MessageBox.Show(
            Application.Current.MainWindow,
            $"プロファイル「{profileName}」を削除しますか？\nこの操作は元に戻せません。",
            "プロファイルの削除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(
            Application.Current.MainWindow,
            message,
            "エラー",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public IReadOnlyList<OscTarget>? ShowSendTargetsWindow(IEnumerable<OscTarget> currentTargets)
    {
        SendTargetsViewModel viewModel = new();
        viewModel.LoadFromModels(currentTargets);
        SendTargetsWindow window = new()
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? viewModel.ToModels() : null;
    }

    public HotkeySettings? ShowHotkeySettingsWindow(HotkeySettings currentSettings)
    {
        HotkeySettingsViewModel viewModel = new();
        viewModel.LoadFromSettings(currentSettings);
        HotkeySettingsWindow window = new()
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? viewModel.ToSettings() : null;
    }

    public KeyRepeatSettings? ShowKeyRepeatSettingsWindow(KeyRepeatSettings currentSettings)
    {
        KeyRepeatSettingsViewModel viewModel = new();
        viewModel.LoadFromSettings(currentSettings);
        KeyRepeatSettingsWindow window = new()
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow,
        };
        return window.ShowDialog() == true ? viewModel.ToSettings() : null;
    }

    public void ShowAboutWindow()
    {
        AboutWindow window = new() { Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }
}
