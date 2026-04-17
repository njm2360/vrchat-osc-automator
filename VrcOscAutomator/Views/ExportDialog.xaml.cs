using System.Windows;

namespace VrcOscAutomator.Views;

public partial class ExportDialog : Window
{
    public ExportDialog(string exportData)
    {
        InitializeComponent();
        OutputTextBox.Text = exportData;
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
