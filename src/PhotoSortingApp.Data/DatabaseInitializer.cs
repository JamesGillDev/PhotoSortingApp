using Microsoft.EntityFrameworkCore;

namespace PhotoSortingApp.Data;

public static class DatabaseInitializer
{
    public static async Task EnsureMigratedAsync(Func<PhotoCatalogDbContext> contextFactory, CancellationToken cancellationToken = default)
    {
        using var db = contextFactory();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
