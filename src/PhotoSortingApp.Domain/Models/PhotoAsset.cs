using PhotoSortingApp.Domain.Enums;

namespace PhotoSortingApp.Domain.Models;

public class PhotoAsset
{
    public int Id { get; set; }

    public int ScanRootId { get; set; }

    public string FullPath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string FolderPath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    // Stored in UTC consistently across the app.
    public DateTime? DateTaken { get; set; }

    public DateTakenSource DateTakenSource { get; set; } = DateTakenSource.Unknown;

    public string? CameraMake { get; set; }

    public string? CameraModel { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string? Sha256 { get; set; }

    public DateTime FileCreatedUtc { get; set; }

    public DateTime FileLastWriteUtc { get; set; }

    public DateTime IndexedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public string? Notes { get; set; }

    public string? TagsCsv { get; set; }

    public string? PeopleCsv { get; set; }

    public string? AnimalsCsv { get; set; }

    public ScanRoot? ScanRoot { get; set; }
}
