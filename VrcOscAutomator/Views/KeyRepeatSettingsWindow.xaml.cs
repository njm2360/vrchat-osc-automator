using System.Windows;

namespace VrcOscAutomator.Views;

public partial class KeyRepeatSettingsWindow : Window
{
    public KeyRepeatSettingsWindow()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
