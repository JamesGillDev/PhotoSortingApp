using PhotoSortingApp.Domain.Enums;

namespace PhotoSortingApp.Domain.Models;

public class PhotoMetadataResult
{
    public DateTime? DateTakenUtc { get; set; }

    public DateTakenSource DateTakenSource { get; set; } = DateTakenSource.Unknown;

    public string? CameraMake { get; set; }

    public string? CameraModel { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }
}
