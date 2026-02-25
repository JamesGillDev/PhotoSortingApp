namespace PhotoSortingApp.Domain.Models;

public class ScanRoot
{
    public int Id { get; set; }

    public string RootPath { get; set; } = string.Empty;

    public DateTime? LastScanUtc { get; set; }

    public int TotalFilesLastScan { get; set; }

    public string? Notes { get; set; }

    public bool EnableDuplicateDetection { get; set; }

    public ICollection<PhotoAsset> Photos { get; set; } = new List<PhotoAsset>();
}
