using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PhotoSortingApp.Core.Infrastructure;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Data.Services;

public class DuplicateService : IDuplicateService
{
    private readonly Func<PhotoCatalogDbContext> _contextFactory;

    public DuplicateService(Func<PhotoCatalogDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<int> ComputeMissingHashesAsync(int scanRootId, IProgress<ScanProgressInfo>? progress = null, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var candidates = await db.PhotoAssets
            .Where(x => x.ScanRootId == scanRootId && (x.Sha256 == null || x.Sha256 == string.Empty))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        var completed = 0;

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(candidate.FullPath))
            {
                continue;
            }

            try
            {
                candidate.Sha256 = await Task.Run(() => Hashing.ComputeSha256ForFile(candidate.FullPath), cancellationToken).ConfigureAwait(false);
                candidate.UpdatedUtc = DateTime.UtcNow;
                completed++;
            }
            catch
            {
                // Skip unreadable files and keep processing.
            }

            progress?.Report(new ScanProgressInfo
            {
                FilesFound = candidates.Count,
                FilesIndexed = completed,
                CurrentFile = candidate.FullPath,
                Elapsed = stopwatch.Elapsed
            });
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return completed;
    }

    public async Task<IReadOnlyList<DuplicateGroup>> GetDuplicateGroupsAsync(int scanRootId, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        return await db.PhotoAssets.AsNoTracking()
            .Where(x => x.ScanRootId == scanRootId && x.Sha256 != null && x.Sha256 != string.Empty)
            .GroupBy(x => x.Sha256!)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup
            {
                Sha256 = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Sha256)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
