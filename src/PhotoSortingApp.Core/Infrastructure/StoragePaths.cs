namespace PhotoSortingApp.Core.Infrastructure;

public static class StoragePaths
{
    public static string GetAppDataDirectory(string? baseDirectory = null)
    {
        var root = baseDirectory ?? AppContext.BaseDirectory;
        return Path.Combine(root, "App_Data");
    }

    public static string GetDatabasePath(string? baseDirectory = null)
    {
        return Path.Combine(GetAppDataDirectory(baseDirectory), "PhotoCatalog.db");
    }

    public static string GetThumbnailCacheDirectory(string? baseDirectory = null)
    {
        return Path.Combine(GetAppDataDirectory(baseDirectory), "ThumbCache");
    }

    public static string GetOrganizerLogPath(string? baseDirectory = null)
    {
        return Path.Combine(GetAppDataDirectory(baseDirectory), "OrganizerPlan.log");
    }

    public static void EnsureDirectories(string? baseDirectory = null)
    {
        Directory.CreateDirectory(GetAppDataDirectory(baseDirectory));
        Directory.CreateDirectory(GetThumbnailCacheDirectory(baseDirectory));
    }
}
