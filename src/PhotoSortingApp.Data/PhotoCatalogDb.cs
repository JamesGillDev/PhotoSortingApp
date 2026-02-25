using Microsoft.EntityFrameworkCore;
using PhotoSortingApp.Core.Infrastructure;

namespace PhotoSortingApp.Data;

public static class PhotoCatalogDb
{
    public static DbContextOptions<PhotoCatalogDbContext> CreateOptions(string? baseDirectory = null)
    {
        StoragePaths.EnsureDirectories(baseDirectory);
        var dbPath = StoragePaths.GetDatabasePath(baseDirectory);

        return new DbContextOptionsBuilder<PhotoCatalogDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .EnableSensitiveDataLogging(false)
            .Options;
    }
}
