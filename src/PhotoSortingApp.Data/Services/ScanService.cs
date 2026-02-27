using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PhotoSortingApp.Core.Infrastructure;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Data.Services;

public class ScanService : IScanService
{
    private const int SaveBatchSize = 100;
    private static readonly HashSet<string> ExcludedTopLevelDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "$Recycle.Bin",
        "Config.Msi",
        "MSOCache",
        "PerfLogs",
        "Program Files",
        "Program Files (x86)",
        "ProgramData",
        "Recovery",
        "System Volume Information",
        "Windows"
    };

    private static readonly HashSet<string> ExcludedSegmentNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".nuget",
        "AppData",
        "node_modules"
    };

    private readonly Func<PhotoCatalogDbContext> _contextFactory;
    private readonly IMetadataService _metadataService;

    public ScanService(Func<PhotoCatalogDbContext> contextFactory, IMetadataService metadataService)
    {
        _contextFactory = contextFactory;
        _metadataService = metadataService;
    }

    public async Task<IReadOnlyList<ScanRoot>> GetScanRootsAsync(CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        return await db.ScanRoots
            .AsNoTracking()
            .OrderBy(x => x.RootPath)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ScanRoot> GetOrCreateScanRootAsync(string rootPath, bool enableDuplicateDetection, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path is required.", nameof(rootPath));
        }

        var normalized = NormalizePath(rootPath);

        using var db = _contextFactory();
        var existing = await db.ScanRoots.SingleOrDefaultAsync(x => x.RootPath == normalized, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (existing.EnableDuplicateDetection != enableDuplicateDetection)
            {
                existing.EnableDuplicateDetection = enableDuplicateDetection;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return existing;
        }

        var entity = new ScanRoot
        {
            RootPath = normalized,
            EnableDuplicateDetection = enableDuplicateDetection
        };
        db.ScanRoots.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public async Task UpdateDuplicateDetectionAsync(int scanRootId, bool enabled, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var root = await db.ScanRoots.SingleAsync(x => x.Id == scanRootId, cancellationToken).ConfigureAwait(false);
        root.EnableDuplicateDetection = enabled;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ScanResult> ScanAsync(
        int scanRootId,
        IProgress<ScanProgressInfo>? progress = null,
        CancellationToken cancellationToken = default,
        ScanOptions? options = null)
    {
        using var db = _contextFactory();
        var root = await db.ScanRoots.SingleAsync(x => x.Id == scanRootId, cancellationToken).ConfigureAwait(false);

        if (!Directory.Exists(root.RootPath))
        {
            throw new DirectoryNotFoundException($"Scan root does not exist: {root.RootPath}");
        }

        var stopwatch = Stopwatch.StartNew();
        var filePaths = EnumerateSupportedFiles(root.RootPath, options ?? new ScanOptions()).ToList();
        var filesFound = filePaths.Count;

        var existingList = await db.PhotoAssets
            .Where(x => x.ScanRootId == scanRootId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingByPath = existingList.ToDictionary(x => x.FullPath, StringComparer.OrdinalIgnoreCase);
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var indexed = 0;
        var updated = 0;
        var skipped = 0;
        var removed = 0;
        var pendingWrites = 0;

        ReportProgress(progress, filesFound, indexed, skipped, updated, removed, string.Empty, stopwatch.Elapsed);

        foreach (var fullPath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!seenPaths.Add(fullPath))
            {
                skipped++;
                continue;
            }

            try
            {
                var info = new FileInfo(fullPath);
                if (!info.Exists)
                {
                    continue;
                }

                if (existingByPath.TryGetValue(fullPath, out var existing))
                {
                    var unchanged = existing.FileSizeBytes == info.Length &&
                                    existing.FileLastWriteUtc == info.LastWriteTimeUtc;

                    if (unchanged)
                    {
                        skipped++;
                    }
                    else
                    {
                        await PopulatePhotoEntityAsync(existing, root, info, isNew: false, cancellationToken).ConfigureAwait(false);
                        updated++;
                        pendingWrites++;
                    }
                }
                else
                {
                    var entity = new PhotoAsset
                    {
                        ScanRootId = scanRootId,
                        FullPath = fullPath
                    };
                    await PopulatePhotoEntityAsync(entity, root, info, isNew: true, cancellationToken).ConfigureAwait(false);
                    db.PhotoAssets.Add(entity);
                    existingByPath[fullPath] = entity;
                    indexed++;
                    pendingWrites++;
                }
            }
            catch
            {
                skipped++;
            }

            if (pendingWrites >= SaveBatchSize)
            {
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                pendingWrites = 0;
            }

            ReportProgress(progress, filesFound, indexed, skipped, updated, removed, fullPath, stopwatch.Elapsed);
        }

        var stale = existingByPath.Values.Where(x => !seenPaths.Contains(x.FullPath)).ToList();
        if (stale.Count > 0)
        {
            db.PhotoAssets.RemoveRange(stale);
            removed = stale.Count;
            pendingWrites += stale.Count;
        }

        root.LastScanUtc = DateTime.UtcNow;
        root.TotalFilesLastScan = filesFound;

        if (pendingWrites > 0 || db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        stopwatch.Stop();
        ReportProgress(progress, filesFound, indexed, skipped, updated, removed, string.Empty, stopwatch.Elapsed);

        return new ScanResult
        {
            FilesFound = filesFound,
            FilesIndexed = indexed,
            FilesUpdated = updated,
            FilesSkipped = skipped,
            FilesRemoved = removed,
            Duration = stopwatch.Elapsed
        };
    }

    private async Task PopulatePhotoEntityAsync(PhotoAsset entity, ScanRoot root, FileInfo fileInfo, bool isNew, CancellationToken cancellationToken)
    {
        var metadata = await _metadataService.ExtractAsync(fileInfo.FullName, cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;

        entity.FileName = fileInfo.Name;
        entity.Extension = fileInfo.Extension.ToLowerInvariant();
        entity.FolderPath = fileInfo.DirectoryName ?? string.Empty;
        entity.FileSizeBytes = fileInfo.Length;
        entity.FileCreatedUtc = fileInfo.CreationTimeUtc;
        entity.FileLastWriteUtc = fileInfo.LastWriteTimeUtc;
        entity.DateTaken = metadata.DateTakenUtc;
        entity.DateTakenSource = metadata.DateTakenSource;
        entity.CameraMake = metadata.CameraMake;
        entity.CameraModel = metadata.CameraModel;
        entity.Width = metadata.Width;
        entity.Height = metadata.Height;
        entity.UpdatedUtc = now;

        if (isNew)
        {
            entity.IndexedUtc = now;
        }

        if (root.EnableDuplicateDetection)
        {
            entity.Sha256 = await Task.Run(() => Hashing.ComputeSha256ForFile(fileInfo.FullName), cancellationToken).ConfigureAwait(false);
        }
        else if (!isNew)
        {
            entity.Sha256 = null;
        }
    }

    private static IEnumerable<string> EnumerateSupportedFiles(string rootPath, ScanOptions options)
    {
        rootPath = Path.GetFullPath(rootPath);
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (ShouldSkipDirectory(current, rootPath, options))
            {
                continue;
            }

            string[] directories;

            try
            {
                directories = Directory.GetDirectories(current);
            }
            catch
            {
                continue;
            }

            foreach (var directory in directories)
            {
                if (ShouldSkipDirectory(directory, rootPath, options))
                {
                    continue;
                }

                pending.Push(directory);
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(current);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                if (SupportedPhotoExtensions.IsSupported(file))
                {
                    yield return file;
                }
            }
        }
    }

    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath);
        if (!string.IsNullOrWhiteSpace(root) &&
            string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool ShouldSkipDirectory(string directoryPath, string rootPath, ScanOptions options)
    {
        try
        {
            var attributes = File.GetAttributes(directoryPath);
            if (options.SkipReparsePoints &&
                (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint &&
                !PathsEqual(directoryPath, rootPath))
            {
                return true;
            }

            if (options.SkipHiddenAndSystemDirectories &&
                !PathsEqual(directoryPath, rootPath) &&
                (((attributes & FileAttributes.Hidden) == FileAttributes.Hidden) ||
                 ((attributes & FileAttributes.System) == FileAttributes.System)))
            {
                return true;
            }
        }
        catch
        {
            return true;
        }

        if (!options.ExcludeLikelySystemAndProgramDirectories)
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(rootPath, directoryPath);
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
        {
            return false;
        }

        var segments = relativePath
            .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        if (ExcludedTopLevelDirectoryNames.Contains(segments[0]))
        {
            return true;
        }

        return segments.Any(segment => ExcludedSegmentNames.Contains(segment));
    }

    private static void ReportProgress(
        IProgress<ScanProgressInfo>? progress,
        int filesFound,
        int filesIndexed,
        int filesSkipped,
        int filesUpdated,
        int filesRemoved,
        string currentFile,
        TimeSpan elapsed)
    {
        if (progress is null)
        {
            return;
        }

        progress.Report(new ScanProgressInfo
        {
            FilesFound = filesFound,
            FilesIndexed = filesIndexed,
            FilesSkipped = filesSkipped,
            FilesUpdated = filesUpdated,
            FilesRemoved = filesRemoved,
            CurrentFile = currentFile,
            Elapsed = elapsed
        });
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);
    }
}
