using System.Windows;
using PhotoSortingApp.App.Services;
using PhotoSortingApp.App.ViewModels;

namespace PhotoSortingApp.App;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new AppServices(AppContext.BaseDirectory);
            await services.InitializeDatabaseAsync().ConfigureAwait(true);

            var viewModel = new MainViewModel(services);
            await viewModel.InitializeAsync().ConfigureAwait(true);

            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Application startup failed.{Environment.NewLine}{ex.Message}",
                "PhotoSortingApp",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
