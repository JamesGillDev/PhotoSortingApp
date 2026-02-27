namespace PhotoSortingApp.Domain.Models;

public class SmartRenameAnalysis
{
    public string SuggestedBaseName { get; set; } = string.Empty;

    public IReadOnlyList<string> SubjectTags { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> DetectedPeople { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> DetectedAnimals { get; set; } = Array.Empty<string>();

    public string? Setting { get; set; }

    public string? Season { get; set; }

    public string? Holiday { get; set; }

    public string? TimeOfDay { get; set; }

    public string? ShotType { get; set; }

    public string? PeopleHint { get; set; }

    public bool UsedVisionModel { get; set; }

    public string? Summary { get; set; }
}
