using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace VrcOscAutomator.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var decoder = new IconBitmapDecoder(
            new Uri("pack://application:,,,/Resources/icon.ico"),
            BitmapCreateOptions.None, BitmapCacheOption.Default);
        AppIcon.Source = decoder.Frames.OrderBy(f => Math.Abs(f.PixelWidth - 128)).First();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is not null ? $"バージョン {version.Major}.{version.Minor}.{version.Build}" : "バージョン不明";
    }

    private void GitHubButton_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("https://github.com/njm2360/vrchat-osc-automator") { UseShellExecute = true });
}
