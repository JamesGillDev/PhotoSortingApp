namespace PhotoSortingApp.Domain.Models;

public class ScanResult
{
    public int FilesFound { get; set; }

    public int FilesIndexed { get; set; }

    public int FilesSkipped { get; set; }

    public int FilesUpdated { get; set; }

    public int FilesRemoved { get; set; }

    public TimeSpan Duration { get; set; }
}
