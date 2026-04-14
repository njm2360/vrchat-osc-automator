using System.Windows;

namespace VrcOscAutomator.Views;

public partial class ExportDialog : Window
{
    public ExportDialog(string base64)
    {
        InitializeComponent();
        OutputTextBox.Text = base64;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(OutputTextBox.Text);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
