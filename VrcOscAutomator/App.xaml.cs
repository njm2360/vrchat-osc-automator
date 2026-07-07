using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Services;
using VrcOscAutomator.ViewModels;
using VrcOscAutomator.Views;

namespace VrcOscAutomator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        ServiceCollection services = new();
        services.AddSingleton<IOscSender, OscSenderService>();
        services.AddSingleton<IKeyboardSender, KeyboardSenderService>();
        services.AddSingleton<IMouseSender, MouseSenderService>();
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
        services.AddSingleton<ISequencePlayer, SequencePlayerService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ISequenceImportExportService, SequenceImportExportService>();
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        services.AddSingleton<MainWindowViewModel>();

        ServiceProvider provider = services.BuildServiceProvider();

        MainWindow window = new()
        {
            DataContext = provider.GetRequiredService<MainWindowViewModel>(),
        };
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"予期しないエラーが発生しました。\n\n{e.Exception.Message}",
            "エラー",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
    }
}
