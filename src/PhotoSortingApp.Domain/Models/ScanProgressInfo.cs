namespace PhotoSortingApp.Domain.Models;

public class ScanProgressInfo
{
    public int FilesFound { get; set; }

    public int FilesIndexed { get; set; }

    public int FilesSkipped { get; set; }

    public int FilesUpdated { get; set; }

    public int FilesRemoved { get; set; }

    public string CurrentFile { get; set; } = string.Empty;

    public TimeSpan Elapsed { get; set; }
}
