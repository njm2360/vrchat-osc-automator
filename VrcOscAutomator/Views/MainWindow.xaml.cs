using System.Windows;
using VrcOscAutomator.ViewModels;

namespace VrcOscAutomator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.LoadedCommand.ExecuteAsync(null);
                vm.InitializeHotkeys(this);
            }
        };

        Closing += async (_, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (vm.IsPlaying)
                {
                    e.Cancel = true;
                    MessageBox.Show(this,
                        "実行中は終了できません。停止してから閉じてください。",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                await vm.ClosingCommand.ExecuteAsync(null);
            }
        };
    }
}
