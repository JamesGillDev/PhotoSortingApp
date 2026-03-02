namespace PhotoSortingApp.Core.Infrastructure;

public static class SupportedPhotoExtensions
{
    private static readonly HashSet<string> SupportedImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".heic",
        ".webp",
        ".gif",
        ".bmp",
        ".tif",
        ".tiff"
    };
    private static readonly HashSet<string> SupportedVideos = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".mov",
        ".m4v",
        ".avi",
        ".mkv",
        ".wmv",
        ".webm"
    };
    private static readonly HashSet<string> Supported = SupportedImages
        .Concat(SupportedVideos)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(string path)
    {
        return Supported.Contains(Path.GetExtension(path));
    }

    public static bool IsImage(string path)
    {
        return SupportedImages.Contains(Path.GetExtension(path));
    }

    public static bool IsVideo(string path)
    {
        return SupportedVideos.Contains(Path.GetExtension(path));
    }

    public static IReadOnlyCollection<string> All => Supported;

    public static IReadOnlyCollection<string> Images => SupportedImages;

    public static IReadOnlyCollection<string> Videos => SupportedVideos;
}
