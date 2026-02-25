namespace PhotoSortingApp.App.ViewModels;

public class ScanRootItemViewModel
{
    public int Id { get; set; }

    public string RootPath { get; set; } = string.Empty;

    public bool EnableDuplicateDetection { get; set; }

    public DateTime? LastScanUtc { get; set; }

    public int TotalFilesLastScan { get; set; }

    public string DisplayName => RootPath;
}
