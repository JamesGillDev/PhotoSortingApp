using PhotoSortingApp.Domain.Enums;

namespace PhotoSortingApp.App.ViewModels;

public class DateSourceOptionViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTakenSource? Value { get; set; }
}
