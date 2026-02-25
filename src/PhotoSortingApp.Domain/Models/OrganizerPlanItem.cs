namespace PhotoSortingApp.Domain.Models;

public class OrganizerPlanItem
{
    public int PhotoId { get; set; }

    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
