namespace PhotoSortingApp.Domain.Models;

public class DuplicateGroup
{
    public string Sha256 { get; set; } = string.Empty;

    public int Count { get; set; }
}
