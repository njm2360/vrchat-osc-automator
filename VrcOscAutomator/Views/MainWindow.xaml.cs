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

    private void TabHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is ProfileViewModel vm)
            vm.BeginRename();
    }

    private void RenameTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectAll();
            tb.Focus();
        }
    }

    private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not ProfileViewModel vm) return;
        if (e.Key == Key.Enter)  { vm.CommitRename(); e.Handled = true; }
        if (e.Key == Key.Escape) { vm.CancelRename(); e.Handled = true; }
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is ProfileViewModel vm)
            vm.CommitRename();
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
