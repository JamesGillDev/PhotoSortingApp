namespace PhotoSortingApp.App.ViewModels;

public class UndoActionItemViewModel
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime TimestampLocal { get; init; } = DateTime.Now;

    public string Description { get; init; } = string.Empty;

    public string DisplayName => $"{TimestampLocal:HH:mm:ss} - {Description}";
}
