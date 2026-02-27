using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PhotoSortingApp.App.Services;
using PhotoSortingApp.App.Theming;
using PhotoSortingApp.App.ViewModels;
using MediaColor = System.Windows.Media.Color;
using WpfSystemColors = System.Windows.SystemColors;

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
            ShowStartupGuide(mainWindow);
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
            SetBrushColor("DisabledForegroundBrush", MediaColor.FromRgb(0xC5, 0xD0, 0xDD));
            SetBrushColor("SelectionBrush", MediaColor.FromRgb(0x4C, 0x74, 0xA8));
            SetBrushColor("ControlPopupBackgroundBrush", MediaColor.FromRgb(0x23, 0x2F, 0x3F));
            SetBrushColor("ControlPopupBorderBrush", MediaColor.FromRgb(0x45, 0x57, 0x70));
            SetBrushColor("ControlHoverBrush", MediaColor.FromRgb(0x2E, 0x3D, 0x52));
            SetBrushColor("ControlPressedBrush", MediaColor.FromRgb(0x39, 0x4B, 0x65));
            SetBrushColor("ControlSelectionBackgroundBrush", MediaColor.FromRgb(0x4C, 0x74, 0xA8));
            SetBrushColor("ControlSelectionForegroundBrush", MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
            SetBrushColor("ControlDisabledBackgroundBrush", MediaColor.FromRgb(0x2A, 0x37, 0x49));
            ApplySystemColorOverrides(
                window: MediaColor.FromRgb(0x23, 0x2F, 0x3F),
                windowText: MediaColor.FromRgb(0xEE, 0xF2, 0xF8),
                control: MediaColor.FromRgb(0x23, 0x2F, 0x3F),
                controlText: MediaColor.FromRgb(0xEE, 0xF2, 0xF8),
                controlLight: MediaColor.FromRgb(0x2E, 0x3D, 0x52),
                controlLightLight: MediaColor.FromRgb(0x3A, 0x4E, 0x67),
                controlDark: MediaColor.FromRgb(0x1B, 0x26, 0x34),
                controlDarkDark: MediaColor.FromRgb(0x14, 0x1E, 0x2A),
                menu: MediaColor.FromRgb(0x23, 0x2F, 0x3F),
                menuText: MediaColor.FromRgb(0xEE, 0xF2, 0xF8),
                grayText: MediaColor.FromRgb(0xA3, 0xB1, 0xC4),
                highlight: MediaColor.FromRgb(0x4C, 0x74, 0xA8),
                highlightText: MediaColor.FromRgb(0xFF, 0xFF, 0xFF),
                inactiveSelection: MediaColor.FromRgb(0x2E, 0x3D, 0x52),
                inactiveSelectionText: MediaColor.FromRgb(0xEE, 0xF2, 0xF8));
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
            SetBrushColor("DisabledForegroundBrush", MediaColor.FromRgb(0x66, 0x70, 0x83));
            SetBrushColor("SelectionBrush", MediaColor.FromRgb(0xCC, 0xE4, 0xFF));
            SetBrushColor("ControlPopupBackgroundBrush", MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
            SetBrushColor("ControlPopupBorderBrush", MediaColor.FromRgb(0xB9, 0xC2, 0xCF));
            SetBrushColor("ControlHoverBrush", MediaColor.FromRgb(0xEA, 0xF3, 0xFF));
            SetBrushColor("ControlPressedBrush", MediaColor.FromRgb(0xDC, 0xEA, 0xFF));
            SetBrushColor("ControlSelectionBackgroundBrush", MediaColor.FromRgb(0x2F, 0x6F, 0xB8));
            SetBrushColor("ControlSelectionForegroundBrush", MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
            SetBrushColor("ControlDisabledBackgroundBrush", MediaColor.FromRgb(0xE6, 0xEB, 0xF2));
            ApplySystemColorOverrides(
                window: MediaColor.FromRgb(0xFF, 0xFF, 0xFF),
                windowText: MediaColor.FromRgb(0x10, 0x18, 0x28),
                control: MediaColor.FromRgb(0xFF, 0xFF, 0xFF),
                controlText: MediaColor.FromRgb(0x10, 0x18, 0x28),
                controlLight: MediaColor.FromRgb(0xF5, 0xF7, 0xFB),
                controlLightLight: MediaColor.FromRgb(0xFF, 0xFF, 0xFF),
                controlDark: MediaColor.FromRgb(0xC2, 0xCC, 0xD9),
                controlDarkDark: MediaColor.FromRgb(0x8B, 0x98, 0xAB),
                menu: MediaColor.FromRgb(0xFF, 0xFF, 0xFF),
                menuText: MediaColor.FromRgb(0x10, 0x18, 0x28),
                grayText: MediaColor.FromRgb(0x8A, 0x94, 0xA6),
                highlight: MediaColor.FromRgb(0x2F, 0x6F, 0xB8),
                highlightText: MediaColor.FromRgb(0xFF, 0xFF, 0xFF),
                inactiveSelection: MediaColor.FromRgb(0xDC, 0xEA, 0xFF),
                inactiveSelectionText: MediaColor.FromRgb(0x10, 0x18, 0x28));
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

    private static void ShowStartupGuide(Window owner)
    {
        try
        {
            var guide = new StartupGuideWindow
            {
                Owner = owner
            };
            guide.ShowDialog();
        }
        catch
        {
            // Non-critical UI helper; ignore if guide fails so main app remains usable.
        }
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

    private void SetBrushColor(object resourceKey, MediaColor color)
    {
        Resources[resourceKey] = new SolidColorBrush(color);
    }

    private void SetColor(object resourceKey, MediaColor color)
    {
        Resources[resourceKey] = color;
    }

    private void ApplySystemColorOverrides(
        MediaColor window,
        MediaColor windowText,
        MediaColor control,
        MediaColor controlText,
        MediaColor controlLight,
        MediaColor controlLightLight,
        MediaColor controlDark,
        MediaColor controlDarkDark,
        MediaColor menu,
        MediaColor menuText,
        MediaColor grayText,
        MediaColor highlight,
        MediaColor highlightText,
        MediaColor inactiveSelection,
        MediaColor inactiveSelectionText)
    {
        SetBrushColor(WpfSystemColors.WindowBrushKey, window);
        SetColor(WpfSystemColors.WindowColorKey, window);
        SetBrushColor(WpfSystemColors.WindowTextBrushKey, windowText);
        SetColor(WpfSystemColors.WindowTextColorKey, windowText);

        SetBrushColor(WpfSystemColors.ControlBrushKey, control);
        SetColor(WpfSystemColors.ControlColorKey, control);
        SetBrushColor(WpfSystemColors.ControlTextBrushKey, controlText);
        SetColor(WpfSystemColors.ControlTextColorKey, controlText);
        SetBrushColor(WpfSystemColors.ControlLightBrushKey, controlLight);
        SetColor(WpfSystemColors.ControlLightColorKey, controlLight);
        SetBrushColor(WpfSystemColors.ControlLightLightBrushKey, controlLightLight);
        SetColor(WpfSystemColors.ControlLightLightColorKey, controlLightLight);
        SetBrushColor(WpfSystemColors.ControlDarkBrushKey, controlDark);
        SetColor(WpfSystemColors.ControlDarkColorKey, controlDark);
        SetBrushColor(WpfSystemColors.ControlDarkDarkBrushKey, controlDarkDark);
        SetColor(WpfSystemColors.ControlDarkDarkColorKey, controlDarkDark);

        SetBrushColor(WpfSystemColors.MenuBrushKey, menu);
        SetColor(WpfSystemColors.MenuColorKey, menu);
        SetBrushColor(WpfSystemColors.MenuTextBrushKey, menuText);
        SetColor(WpfSystemColors.MenuTextColorKey, menuText);

        SetBrushColor(WpfSystemColors.HighlightBrushKey, highlight);
        SetColor(WpfSystemColors.HighlightColorKey, highlight);
        SetBrushColor(WpfSystemColors.HighlightTextBrushKey, highlightText);
        SetColor(WpfSystemColors.HighlightTextColorKey, highlightText);
        SetBrushColor(WpfSystemColors.GrayTextBrushKey, grayText);
        SetColor(WpfSystemColors.GrayTextColorKey, grayText);
        SetBrushColor(WpfSystemColors.InactiveSelectionHighlightBrushKey, inactiveSelection);
        SetBrushColor(WpfSystemColors.InactiveSelectionHighlightTextBrushKey, inactiveSelectionText);
    }
}
