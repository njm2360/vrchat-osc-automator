using System.Windows;

namespace VrcOscAutomator.Views;

public partial class ImportDialog : Window
{
    public string? InputText { get; private set; }

    public ImportDialog()
    {
        InitializeComponent();
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        InputText = InputTextBox.Text.Trim();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
