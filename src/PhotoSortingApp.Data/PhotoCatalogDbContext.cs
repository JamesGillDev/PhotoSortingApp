using Microsoft.EntityFrameworkCore;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Data;

public class PhotoCatalogDbContext : DbContext
{
    public PhotoCatalogDbContext(DbContextOptions<PhotoCatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<PhotoAsset> PhotoAssets => Set<PhotoAsset>();

    public DbSet<ScanRoot> ScanRoots => Set<ScanRoot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ScanRoot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RootPath).IsRequired();
            entity.HasIndex(x => x.RootPath).IsUnique();
            entity.Property(x => x.Notes).HasMaxLength(2000);
        });

        modelBuilder.Entity<PhotoAsset>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullPath).IsRequired();
            entity.Property(x => x.FileName).IsRequired();
            entity.Property(x => x.Extension).IsRequired();
            entity.Property(x => x.FolderPath).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.Property(x => x.TagsCsv).HasMaxLength(2000);

            entity.HasIndex(x => x.FullPath).IsUnique();
            entity.HasIndex(x => x.ScanRootId);
            entity.HasIndex(x => x.DateTaken);
            entity.HasIndex(x => x.Sha256);

            entity.HasOne(x => x.ScanRoot)
                .WithMany(x => x.Photos)
                .HasForeignKey(x => x.ScanRootId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
