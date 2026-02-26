using PhotoSortingApp.App.Theming;

namespace PhotoSortingApp.App.ViewModels;

public class ThemeOptionViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public AppThemePreference Value { get; set; }
}
