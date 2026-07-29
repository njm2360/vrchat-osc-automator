using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
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
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);

        if (e.Args.Contains("--software-rendering", StringComparer.OrdinalIgnoreCase))
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

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
        e.Handled = true;
        HandleFatal("DispatcherUnhandledException", e.Exception);
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        CrashLogger.Log("AppDomainUnhandledException", e.ExceptionObject as Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashLogger.Log("UnobservedTaskException", e.Exception);
    }

    private void HandleFatal(string source, Exception ex)
    {
        CrashLogger.Log(source, ex);

        MessageBox.Show(
            $"予期しないエラーが発生しました。アプリケーションを終了します。\n\n{ex.Message}\n\n" +
            $"エラーログ\n{CrashLogger.LogPath}",
            "エラー",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        Shutdown(1);
    }
}
