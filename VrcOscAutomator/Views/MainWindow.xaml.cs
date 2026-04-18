using System.Collections.Specialized;
using System.Diagnostics;
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

    private void HelpGitHub_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("https://github.com/njm2360/vrchat-osc-automator") { UseShellExecute = true });

    private void HelpManual_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("https://github.com/njm2360/vrchat-osc-automator/blob/main/docs/user-manual.md") { UseShellExecute = true });

    private void HelpLicense_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("https://github.com/njm2360/vrchat-osc-automator/blob/main/THIRD_PARTY_NOTICES.md") { UseShellExecute = true });

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;

        int index = e.Key switch
        {
            Key.D1 => 0, Key.D2 => 1, Key.D3 => 2,
            Key.D4 => 3, Key.D5 => 4, Key.D6 => 5,
            Key.D7 => 6, Key.D8 => 7, Key.D9 => 8,
            Key.D0 => 9,
            _ => -1,
        };

        if (index >= 0 && DataContext is MainWindowViewModel vm && index < vm.Profiles.Count)
        {
            vm.SelectedProfileIndex = index;
            e.Handled = true;
        }
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
