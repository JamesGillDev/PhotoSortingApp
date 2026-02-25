namespace PhotoSortingApp.Domain.Models;

public class FaceCluster
{
    public string ClusterId { get; set; } = string.Empty;

    public IReadOnlyList<int> PhotoIds { get; set; } = Array.Empty<int>();
}
