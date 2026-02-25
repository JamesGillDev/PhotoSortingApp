using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PhotoSortingApp.Core.Infrastructure;

namespace PhotoSortingApp.Data;

public class PhotoCatalogDbContextFactory : IDesignTimeDbContextFactory<PhotoCatalogDbContext>
{
    public PhotoCatalogDbContext CreateDbContext(string[] args)
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        var appDataPath = StoragePaths.GetAppDataDirectory(workingDirectory);
        Directory.CreateDirectory(appDataPath);
        var dbPath = Path.Combine(appDataPath, "PhotoCatalog.db");

        var optionsBuilder = new DbContextOptionsBuilder<PhotoCatalogDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new PhotoCatalogDbContext(optionsBuilder.Options);
    }
}
