namespace PhotoSortingApp.Core.Infrastructure;

public static class SupportedPhotoExtensions
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".heic"
    };

    public static bool IsSupported(string path)
    {
        return Supported.Contains(Path.GetExtension(path));
    }

    public static IReadOnlyCollection<string> All => Supported;
}
