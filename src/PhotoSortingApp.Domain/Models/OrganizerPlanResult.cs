namespace PhotoSortingApp.Domain.Models;

public class OrganizerPlanResult
{
    public DateTime GeneratedUtc { get; set; }

    public int TotalEvaluated { get; set; }

    public int TotalMoves { get; set; }

    public IReadOnlyList<OrganizerPlanItem> Items { get; set; } = Array.Empty<OrganizerPlanItem>();
}
