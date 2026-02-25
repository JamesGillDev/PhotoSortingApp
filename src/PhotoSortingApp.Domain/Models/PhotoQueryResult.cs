namespace PhotoSortingApp.Domain.Models;

public class PhotoQueryResult
{
    public IReadOnlyList<PhotoAsset> Items { get; set; } = Array.Empty<PhotoAsset>();

    public int TotalCount { get; set; }
}
