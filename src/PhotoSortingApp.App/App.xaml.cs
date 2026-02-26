using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PhotoSortingApp.App.Services;
using PhotoSortingApp.App.Theming;
using PhotoSortingApp.App.ViewModels;
using MediaColor = System.Windows.Media.Color;

namespace PhotoSortingApp.App;

public partial class App : System.Windows.Application
{
    private AppThemePreference _themePreference = AppThemePreference.System;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
            ApplyTheme(AppThemePreference.System);

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

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
        base.OnExit(e);
    }

    public void ApplyTheme(AppThemePreference preference)
    {
        _themePreference = preference;
        var useDarkTheme = preference switch
        {
            AppThemePreference.Dark => true,
            AppThemePreference.Light => false,
            _ => IsSystemInDarkMode()
        };

        if (useDarkTheme)
        {
            SetBrushColor("AppBackgroundBrush", MediaColor.FromRgb(0x11, 0x17, 0x20));
            SetBrushColor("SurfaceBrush", MediaColor.FromRgb(0x1A, 0x23, 0x31));
            SetBrushColor("SurfaceMutedBrush", MediaColor.FromRgb(0x24, 0x30, 0x40));
            SetBrushColor("SurfaceBorderBrush", MediaColor.FromRgb(0x36, 0x46, 0x5B));
            SetBrushColor("AccentSoftBrush", MediaColor.FromRgb(0x1F, 0x31, 0x4D));
            SetBrushColor("AccentSoftBorderBrush", MediaColor.FromRgb(0x3C, 0x5A, 0x82));
            SetBrushColor("TileBackgroundBrush", MediaColor.FromRgb(0x1E, 0x2A, 0x3A));
            SetBrushColor("TileBorderBrush", MediaColor.FromRgb(0x37, 0x4A, 0x61));
            SetBrushColor("TextPrimaryBrush", MediaColor.FromRgb(0xEE, 0xF2, 0xF8));
            SetBrushColor("TextSecondaryBrush", MediaColor.FromRgb(0xC2, 0xCC, 0xD8));
            SetBrushColor("ControlBackgroundBrush", MediaColor.FromRgb(0x23, 0x2F, 0x3F));
            SetBrushColor("ControlBorderBrush", MediaColor.FromRgb(0x45, 0x57, 0x70));
            SetBrushColor("ControlForegroundBrush", MediaColor.FromRgb(0xEE, 0xF2, 0xF8));
            SetBrushColor("DisabledForegroundBrush", MediaColor.FromRgb(0x8D, 0x9A, 0xAD));
            SetBrushColor("SelectionBrush", MediaColor.FromRgb(0x4C, 0x74, 0xA8));
        }
        else
        {
            SetBrushColor("AppBackgroundBrush", MediaColor.FromRgb(0xF7, 0xF8, 0xFA));
            SetBrushColor("SurfaceBrush", MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
            SetBrushColor("SurfaceMutedBrush", MediaColor.FromRgb(0xF1, 0xF4, 0xF8));
            SetBrushColor("SurfaceBorderBrush", MediaColor.FromRgb(0xD7, 0xDC, 0xE4));
            SetBrushColor("AccentSoftBrush", MediaColor.FromRgb(0xEA, 0xF3, 0xFF));
            SetBrushColor("AccentSoftBorderBrush", MediaColor.FromRgb(0xC5, 0xDA, 0xFF));
            SetBrushColor("TileBackgroundBrush", MediaColor.FromRgb(0xFD, 0xFD, 0xFE));
            SetBrushColor("TileBorderBrush", MediaColor.FromRgb(0xE0, 0xE5, 0xED));
            SetBrushColor("TextPrimaryBrush", MediaColor.FromRgb(0x10, 0x18, 0x28));
            SetBrushColor("TextSecondaryBrush", MediaColor.FromRgb(0x4A, 0x55, 0x68));
            SetBrushColor("ControlBackgroundBrush", MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
            SetBrushColor("ControlBorderBrush", MediaColor.FromRgb(0xB9, 0xC2, 0xCF));
            SetBrushColor("ControlForegroundBrush", MediaColor.FromRgb(0x10, 0x18, 0x28));
            SetBrushColor("DisabledForegroundBrush", MediaColor.FromRgb(0x8A, 0x94, 0xA6));
            SetBrushColor("SelectionBrush", MediaColor.FromRgb(0xCC, 0xE4, 0xFF));
        }
    }

    private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_themePreference != AppThemePreference.System)
        {
            return;
        }

        if (e.Category != UserPreferenceCategory.General &&
            e.Category != UserPreferenceCategory.Color &&
            e.Category != UserPreferenceCategory.VisualStyle)
        {
            return;
        }

        Dispatcher.Invoke(() => ApplyTheme(AppThemePreference.System));
    }

    private static bool IsSystemInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int appsUseLightTheme)
            {
                return appsUseLightTheme == 0;
            }
        }
        catch
        {
            // Ignore registry read failures and keep light mode.
        }

        return false;
    }

    private void SetBrushColor(string resourceKey, MediaColor color)
    {
        if (Resources[resourceKey] is SolidColorBrush brush)
        {
            brush.Color = color;
            return;
        }

        Resources[resourceKey] = new SolidColorBrush(color);
    }
}
