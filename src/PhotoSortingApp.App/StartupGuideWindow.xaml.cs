using System.Windows;

namespace PhotoSortingApp.App;

public partial class StartupGuideWindow : Window
{
    public StartupGuideWindow()
    {
        InitializeComponent();
    }

    private void OnGotItClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
