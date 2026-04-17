using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VrcOscAutomator.ViewModels;

namespace VrcOscAutomator.Behaviors;

public static class RenameBehavior
{
    // ── TabHeader: ダブルクリックでリネーム ──────────────────────────

    public static readonly DependencyProperty EnableDoubleClickRenameProperty =
        DependencyProperty.RegisterAttached(
            "EnableDoubleClickRename",
            typeof(bool),
            typeof(RenameBehavior),
            new PropertyMetadata(false, OnEnableDoubleClickRenameChanged));

    public static bool GetEnableDoubleClickRename(DependencyObject obj) =>
        (bool)obj.GetValue(EnableDoubleClickRenameProperty);

    public static void SetEnableDoubleClickRename(DependencyObject obj, bool value) =>
        obj.SetValue(EnableDoubleClickRenameProperty, value);

    private static void OnEnableDoubleClickRenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el) return;
        if ((bool)e.NewValue)
            el.MouseLeftButtonDown += OnTabHeaderMouseLeftButtonDown;
        else
            el.MouseLeftButtonDown -= OnTabHeaderMouseLeftButtonDown;
    }

    private static void OnTabHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is ProfileViewModel vm)
            vm.BeginRename();
    }

    // ── RenameTextBox: フォーカス・Enter/Escape/LostFocus ────────────────

    public static readonly DependencyProperty IsRenameBoxProperty =
        DependencyProperty.RegisterAttached(
            "IsRenameBox",
            typeof(bool),
            typeof(RenameBehavior),
            new PropertyMetadata(false, OnIsRenameBoxChanged));

    public static bool GetIsRenameBox(DependencyObject obj) =>
        (bool)obj.GetValue(IsRenameBoxProperty);

    public static void SetIsRenameBox(DependencyObject obj, bool value) =>
        obj.SetValue(IsRenameBoxProperty, value);

    // TextBoxインスタンスごとにウィンドウのPreviewMouseDownハンドラを保持
    private static readonly Dictionary<TextBox, MouseButtonEventHandler> _outsideClickHandlers = new();

    private static void OnIsRenameBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        if ((bool)e.NewValue)
        {
            tb.IsVisibleChanged += OnRenameBoxVisibleChanged;
            tb.LostFocus += OnRenameBoxLostFocus;
            tb.KeyDown += OnRenameBoxKeyDown;
        }
        else
        {
            tb.IsVisibleChanged -= OnRenameBoxVisibleChanged;
            tb.LostFocus -= OnRenameBoxLostFocus;
            tb.KeyDown -= OnRenameBoxKeyDown;
        }
    }

    private static void OnRenameBoxVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var window = Window.GetWindow(tb);
        if (window == null) return;

        if ((bool)e.NewValue)
        {
            // 表示された: フォーカスを当てて外部クリック検知を開始
            tb.Dispatcher.BeginInvoke(() => { tb.SelectAll(); tb.Focus(); });

            MouseButtonEventHandler handler = (_, _) =>
            {
                if (tb.IsMouseOver) return;
                if (tb.DataContext is ProfileViewModel vm) vm.CommitRename();
            };
            _outsideClickHandlers[tb] = handler;
            window.PreviewMouseDown += handler;
        }
        else
        {
            // 非表示になった: ハンドラを解除
            if (_outsideClickHandlers.TryGetValue(tb, out var handler))
            {
                window.PreviewMouseDown -= handler;
                _outsideClickHandlers.Remove(tb);
            }
        }
    }

    private static void OnRenameBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is ProfileViewModel vm)
            vm.CommitRename();
    }

    private static void OnRenameBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not ProfileViewModel vm) return;
        if (e.Key == Key.Enter) { vm.CommitRename(); e.Handled = true; }
        if (e.Key == Key.Escape) { vm.CancelRename(); e.Handled = true; }
    }
}
