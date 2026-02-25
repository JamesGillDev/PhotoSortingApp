namespace PhotoSortingApp.App.ViewModels;

public class SmartAlbumItemViewModel
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public string DisplayName => $"{Name} ({Count})";
}
