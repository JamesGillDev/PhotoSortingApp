namespace PhotoSortingApp.Domain.Models;

public class ScanOptions
{
    public bool ExcludeLikelySystemAndProgramDirectories { get; set; }

    public bool SkipHiddenAndSystemDirectories { get; set; }

    public bool SkipReparsePoints { get; set; }

    public static ScanOptions WholeComputerSafeDefaults => new()
    {
        ExcludeLikelySystemAndProgramDirectories = true,
        SkipHiddenAndSystemDirectories = true,
        SkipReparsePoints = true
    };
}
