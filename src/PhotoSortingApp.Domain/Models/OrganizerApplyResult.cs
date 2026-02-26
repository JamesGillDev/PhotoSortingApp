namespace PhotoSortingApp.Domain.Models;

public class OrganizerApplyResult
{
    public int AttemptedMoves { get; set; }

    public int Moved { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
