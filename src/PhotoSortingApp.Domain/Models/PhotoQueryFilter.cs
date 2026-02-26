using PhotoSortingApp.Domain.Enums;

namespace PhotoSortingApp.Domain.Models;

public class PhotoQueryFilter
{
    public int? ScanRootId { get; set; }

    public string? SearchText { get; set; }

    public DateTime? FromDateUtc { get; set; }

    public DateTime? ToDateUtc { get; set; }

    public DateTakenSource? DateSource { get; set; }

    public string? FolderSubpath { get; set; }

    public string? AlbumKey { get; set; }

    public PhotoSortOption SortBy { get; set; } = PhotoSortOption.DateTakenNewest;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 120;
}
