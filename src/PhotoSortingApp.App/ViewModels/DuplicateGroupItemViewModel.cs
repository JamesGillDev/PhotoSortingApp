namespace PhotoSortingApp.App.ViewModels;

public class DuplicateGroupItemViewModel
{
    public string Sha256 { get; set; } = string.Empty;

    public int Count { get; set; }

    public string DisplayName => $"{Sha256[..Math.Min(10, Sha256.Length)]}... ({Count})";
}
