using System.Text;
using Microsoft.EntityFrameworkCore;
using PhotoSortingApp.Core.Infrastructure;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Data.Services;

public class OrganizerPlanService : IOrganizerPlanService
{
    private const int SaveBatchSize = 50;

    private readonly Func<PhotoCatalogDbContext> _contextFactory;
    private readonly string _logPath;

    public OrganizerPlanService(Func<PhotoCatalogDbContext> contextFactory, string? baseDirectory = null)
    {
        _contextFactory = contextFactory;
        StoragePaths.EnsureDirectories(baseDirectory);
        _logPath = StoragePaths.GetOrganizerLogPath(baseDirectory);
    }

    public async Task<OrganizerPlanResult> CreatePlanAsync(OrganizerPlanRequest request, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var root = await db.ScanRoots.AsNoTracking()
            .SingleAsync(x => x.Id == request.ScanRootId, cancellationToken)
            .ConfigureAwait(false);
        var photos = await db.PhotoAssets.AsNoTracking()
            .Where(x => x.ScanRootId == request.ScanRootId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var occupiedTargets = new HashSet<string>(photos.Select(x => x.FullPath), StringComparer.OrdinalIgnoreCase);
        var planItems = new List<OrganizerPlanItem>();

        foreach (var photo in photos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(photo.FullPath) || string.IsNullOrWhiteSpace(photo.FileName))
            {
                continue;
            }

            var effectiveDateUtc = photo.DateTaken ?? photo.FileLastWriteUtc;
            var effectiveLocal = effectiveDateUtc.ToLocalTime();
            var yearFolder = effectiveLocal.Year.ToString("0000");
            var monthFolder = $"{effectiveLocal.Year:0000}-{effectiveLocal.Month:00}";
            var targetDirectory = Path.Combine(root.RootPath, yearFolder, monthFolder);
            var initialTarget = Path.Combine(targetDirectory, photo.FileName);

            if (PathsEqual(photo.FullPath, initialTarget))
            {
                continue;
            }

            occupiedTargets.Remove(photo.FullPath);
            var resolvedTarget = ResolveUniqueTarget(initialTarget, occupiedTargets);
            occupiedTargets.Add(resolvedTarget);

            planItems.Add(new OrganizerPlanItem
            {
                PhotoId = photo.Id,
                SourcePath = photo.FullPath,
                DestinationPath = resolvedTarget,
                Reason = "Sort to Year/Month folders"
            });
        }

        var result = new OrganizerPlanResult
        {
            GeneratedUtc = DateTime.UtcNow,
            TotalEvaluated = photos.Count,
            TotalMoves = planItems.Count,
            Items = planItems
        };

        await AppendPlanLogAsync(root.RootPath, result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<OrganizerApplyResult> ApplyPlanAsync(
        int scanRootId,
        IReadOnlyList<OrganizerPlanItem> planItems,
        CancellationToken cancellationToken = default)
    {
        if (planItems.Count == 0)
        {
            return new OrganizerApplyResult();
        }

        using var db = _contextFactory();
        var root = await db.ScanRoots
            .SingleAsync(x => x.Id == scanRootId, cancellationToken)
            .ConfigureAwait(false);

        var photoIds = planItems
            .Select(x => x.PhotoId)
            .Distinct()
            .ToList();
        var assets = await db.PhotoAssets
            .Where(x => x.ScanRootId == scanRootId && photoIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var errors = new List<string>();
        var attempted = 0;
        var moved = 0;
        var skipped = 0;
        var failed = 0;
        var pendingWrites = 0;
        var occupiedTargets = new HashSet<string>(
            assets.Values.Select(x => x.FullPath),
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in planItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempted++;

            if (string.IsNullOrWhiteSpace(item.SourcePath) || string.IsNullOrWhiteSpace(item.DestinationPath))
            {
                skipped++;
                errors.Add($"Skipped photo {item.PhotoId}: missing source or destination path.");
                continue;
            }

            var sourcePath = Path.GetFullPath(item.SourcePath);
            var destinationPath = Path.GetFullPath(item.DestinationPath);
            if (PathsEqual(sourcePath, destinationPath))
            {
                skipped++;
                continue;
            }

            if (!File.Exists(sourcePath))
            {
                skipped++;
                errors.Add($"Skipped photo {item.PhotoId}: source file not found -> {sourcePath}");
                continue;
            }

            try
            {
                var targetDirectory = Path.GetDirectoryName(destinationPath);
                if (string.IsNullOrWhiteSpace(targetDirectory))
                {
                    skipped++;
                    errors.Add($"Skipped photo {item.PhotoId}: invalid destination path -> {destinationPath}");
                    continue;
                }

                Directory.CreateDirectory(targetDirectory);
                if (occupiedTargets.Contains(destinationPath) || File.Exists(destinationPath))
                {
                    destinationPath = ResolveUniqueTarget(destinationPath, occupiedTargets);
                }

                File.Move(sourcePath, destinationPath);
                occupiedTargets.Remove(sourcePath);
                occupiedTargets.Add(destinationPath);

                if (assets.TryGetValue(item.PhotoId, out var asset))
                {
                    var destinationInfo = new FileInfo(destinationPath);
                    asset.FullPath = destinationPath;
                    asset.FileName = Path.GetFileName(destinationPath);
                    asset.Extension = Path.GetExtension(destinationPath).ToLowerInvariant();
                    asset.FolderPath = Path.GetDirectoryName(destinationPath) ?? string.Empty;
                    asset.FileCreatedUtc = destinationInfo.CreationTimeUtc;
                    asset.FileLastWriteUtc = destinationInfo.LastWriteTimeUtc;
                    asset.UpdatedUtc = DateTime.UtcNow;
                    pendingWrites++;
                }

                moved++;
                if (pendingWrites >= SaveBatchSize)
                {
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    pendingWrites = 0;
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Failed photo {item.PhotoId}: {ex.Message}");
            }
        }

        if (pendingWrites > 0 || db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var result = new OrganizerApplyResult
        {
            AttemptedMoves = attempted,
            Moved = moved,
            Skipped = skipped,
            Failed = failed,
            Errors = errors
        };

        await AppendApplyLogAsync(root.RootPath, result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task AppendPlanLogAsync(string rootPath, OrganizerPlanResult result, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[{result.GeneratedUtc:O}] Root={rootPath} Evaluated={result.TotalEvaluated} Moves={result.TotalMoves}");
        foreach (var item in result.Items)
        {
            builder.AppendLine($"PLAN: {item.SourcePath} => {item.DestinationPath}");
        }

        await File.AppendAllTextAsync(_logPath, builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private async Task AppendApplyLogAsync(string rootPath, OrganizerApplyResult result, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            $"[{DateTime.UtcNow:O}] APPLY Root={rootPath} Attempted={result.AttemptedMoves} Moved={result.Moved} Skipped={result.Skipped} Failed={result.Failed}");
        foreach (var error in result.Errors.Take(50))
        {
            builder.AppendLine($"APPLY-ERROR: {error}");
        }

        await File.AppendAllTextAsync(_logPath, builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveUniqueTarget(string path, HashSet<string> occupied)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        var candidate = path;
        var suffix = 1;
        while (occupied.Contains(candidate) || File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{fileNameWithoutExtension}_{suffix}{extension}");
            suffix++;
        }

        return candidate;
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);
    }
}
