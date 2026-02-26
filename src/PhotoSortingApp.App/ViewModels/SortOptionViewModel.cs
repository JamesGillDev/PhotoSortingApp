using PhotoSortingApp.Domain.Enums;

namespace PhotoSortingApp.App.ViewModels;

public class SortOptionViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public PhotoSortOption Value { get; set; }
}
