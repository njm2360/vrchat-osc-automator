using System.Windows;
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

        ServiceCollection services = new();
        services.AddSingleton<IOscSender, OscSenderService>();
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
}
