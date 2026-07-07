using System.Windows;
using VrcOscAutomator.ViewModels;

namespace VrcOscAutomator.Views;

public partial class SendTargetsWindow : Window
{
    public SendTargetsWindow()
    {
        InitializeComponent();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SendTargetsViewModel vm)
        {
            string? error = vm.GetInvalidIpError() ?? vm.GetDuplicateError();
            if (error is not null)
            {
                MessageBox.Show(this, error, "保存できません", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
