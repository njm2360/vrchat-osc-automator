using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace VrcOscAutomator.Behaviors;

/// <summary>
/// 数値バインディング TextBox 向けのビヘイビア。
/// ・RefreshOnLostFocus: フォーカスが外れた際に表示をソース値から再描画する。
/// ・MinValue: フォーカスが外れた際に最小値未満の入力をクランプしてソースに書き戻す。
/// </summary>
public static class NumericTextBoxBehavior
{
    private const int NoMinValue = int.MinValue;
    private const int NoMaxValue = int.MaxValue;

    // ── RefreshOnLostFocus ──────────────────────────────────────────────
    public static readonly DependencyProperty RefreshOnLostFocusProperty =
        DependencyProperty.RegisterAttached(
            "RefreshOnLostFocus",
            typeof(bool),
            typeof(NumericTextBoxBehavior),
            new PropertyMetadata(false, OnAnyPropertyChanged));

    public static bool GetRefreshOnLostFocus(TextBox element) =>
        (bool)element.GetValue(RefreshOnLostFocusProperty);

    public static void SetRefreshOnLostFocus(TextBox element, bool value) =>
        element.SetValue(RefreshOnLostFocusProperty, value);

    // ── MinValue ────────────────────────────────────────────────────────
    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.RegisterAttached(
            "MinValue",
            typeof(int),
            typeof(NumericTextBoxBehavior),
            new PropertyMetadata(NoMinValue, OnAnyPropertyChanged));

    public static int GetMinValue(TextBox element) =>
        (int)element.GetValue(MinValueProperty);

    public static void SetMinValue(TextBox element, int value) =>
        element.SetValue(MinValueProperty, value);

    // ── MaxValue ────────────────────────────────────────────────────────
    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.RegisterAttached(
            "MaxValue",
            typeof(int),
            typeof(NumericTextBoxBehavior),
            new PropertyMetadata(NoMaxValue, OnAnyPropertyChanged));

    public static int GetMaxValue(TextBox element) =>
        (int)element.GetValue(MaxValueProperty);

    public static void SetMaxValue(TextBox element, int value) =>
        element.SetValue(MaxValueProperty, value);

    // ── 共通ハンドラ管理 ─────────────────────────────────────────────────
    private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        // 二重登録を防ぐため一旦外してから条件に応じて付け直す
        tb.LostFocus -= OnLostFocus;
        if (GetRefreshOnLostFocus(tb) || GetMinValue(tb) != NoMinValue || GetMaxValue(tb) != NoMaxValue)
            tb.LostFocus += OnLostFocus;
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var expr = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
        if (expr == null) return;

        int min = GetMinValue(tb);
        int max = GetMaxValue(tb);
        if (int.TryParse(tb.Text, out int parsed))
        {
            int clamped = Math.Clamp(parsed, min == NoMinValue ? int.MinValue : min,
                                             max == NoMaxValue ? int.MaxValue : max);
            if (clamped != parsed)
                tb.Text = clamped.ToString();
            // 正常値・クランプ後どちらも UpdateSource でソースに確定させる
            // (LostFocus バインディングではビヘイビアがコミットより先に動くため
            //  UpdateTarget を使うと未コミットの旧値に戻ってしまう)
            expr.UpdateSource();
        }
        else
        {
            // 空文字など変換不能 → ソースから旧値を復元して表示を確定させる
            expr.UpdateTarget();
        }
    }
}
