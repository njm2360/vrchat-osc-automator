using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

                vm.Profiles.CollectionChanged += OnProfilesCollectionChanged;
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

    private void OnProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (ProfileTabControl.Template.FindName("TabStripScrollViewer", ProfileTabControl) is ScrollViewer sv)
                sv.ScrollToRightEnd();
        });
    }

    private void TabControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is TabControl tc &&
            tc.Template.FindName("TabStripScrollViewer", tc) is ScrollViewer sv &&
            sv.IsMouseOver)
        {
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }
}
