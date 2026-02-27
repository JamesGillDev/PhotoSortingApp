using Microsoft.EntityFrameworkCore;
using PhotoSortingApp.Core.Infrastructure;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Data.Services;

public class PhotoEditService : IPhotoEditService
{
    private readonly Func<PhotoCatalogDbContext> _contextFactory;

    public PhotoEditService(Func<PhotoCatalogDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PhotoAsset?> GetPhotoByIdAsync(int photoId, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        return await db.PhotoAssets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PhotoAsset?> RenamePhotoAsync(int photoId, string requestedFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestedFileName))
        {
            throw new ArgumentException("New file name is required.", nameof(requestedFileName));
        }

        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .Include(x => x.ScanRoot)
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        var requestedName = Path.GetFileName(requestedFileName.Trim());
        var incomingExt = Path.GetExtension(requestedName);
        var extension = string.IsNullOrWhiteSpace(incomingExt)
            ? NormalizeExtension(asset.Extension)
            : NormalizeExtension(incomingExt);
        var baseName = Path.GetFileNameWithoutExtension(requestedName);
        var normalizedBase = NormalizeFileName(baseName);
        if (string.IsNullOrWhiteSpace(normalizedBase))
        {
            throw new InvalidOperationException("The requested file name is invalid.");
        }

        var destinationPath = Path.Combine(asset.FolderPath, $"{normalizedBase}{extension}");
        return await RelocatePhotoInternalAsync(db, asset, destinationPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PhotoAsset?> MovePhotoAsync(int photoId, string destinationFolderPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationFolderPath))
        {
            throw new ArgumentException("Destination folder is required.", nameof(destinationFolderPath));
        }

        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .Include(x => x.ScanRoot)
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        var targetFolder = Path.GetFullPath(destinationFolderPath);
        var destinationPath = Path.Combine(targetFolder, asset.FileName);
        return await RelocatePhotoInternalAsync(db, asset, destinationPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PhotoAsset?> RelocatePhotoAsync(int photoId, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Destination path is required.", nameof(destinationPath));
        }

        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .Include(x => x.ScanRoot)
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        return await RelocatePhotoInternalAsync(db, asset, destinationPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PhotoAsset?> CopyPhotoAsync(int photoId, string destinationFolderPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationFolderPath))
        {
            throw new ArgumentException("Destination folder is required.", nameof(destinationFolderPath));
        }

        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .Include(x => x.ScanRoot)
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        var sourcePath = Path.GetFullPath(asset.FullPath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Source file was not found.", sourcePath);
        }

        EnsureInsideRoot(destinationFolderPath, asset.ScanRoot?.RootPath);
        var destinationFolder = Path.GetFullPath(destinationFolderPath);
        Directory.CreateDirectory(destinationFolder);
        var destinationPath = Path.Combine(destinationFolder, asset.FileName);
        destinationPath = ResolveUniquePath(destinationPath);

        File.Copy(sourcePath, destinationPath);

        var destinationInfo = new FileInfo(destinationPath);
        var now = DateTime.UtcNow;
        var copy = new PhotoAsset
        {
            ScanRootId = asset.ScanRootId,
            FullPath = destinationPath,
            FileName = destinationInfo.Name,
            Extension = destinationInfo.Extension.ToLowerInvariant(),
            FolderPath = destinationInfo.DirectoryName ?? string.Empty,
            FileSizeBytes = destinationInfo.Length,
            DateTaken = asset.DateTaken,
            DateTakenSource = asset.DateTakenSource,
            CameraMake = asset.CameraMake,
            CameraModel = asset.CameraModel,
            Width = asset.Width,
            Height = asset.Height,
            Sha256 = asset.Sha256,
            FileCreatedUtc = destinationInfo.CreationTimeUtc,
            FileLastWriteUtc = destinationInfo.LastWriteTimeUtc,
            IndexedUtc = now,
            UpdatedUtc = now,
            Notes = asset.Notes,
            TagsCsv = asset.TagsCsv,
            PeopleCsv = asset.PeopleCsv,
            AnimalsCsv = asset.AnimalsCsv
        };

        db.PhotoAssets.Add(copy);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return copy;
    }

    public async Task<PhotoAsset?> DuplicatePhotoAsync(int photoId, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .Include(x => x.ScanRoot)
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        var sourcePath = Path.GetFullPath(asset.FullPath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Source file was not found.", sourcePath);
        }

        EnsureInsideRoot(asset.FolderPath, asset.ScanRoot?.RootPath);
        Directory.CreateDirectory(asset.FolderPath);

        var baseName = NormalizeFileName(Path.GetFileNameWithoutExtension(asset.FileName));
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "photo";
        }

        var extension = NormalizeExtension(asset.Extension);
        var destinationPath = Path.Combine(asset.FolderPath, $"{baseName}_copy{extension}");
        destinationPath = ResolveUniquePath(destinationPath);
        File.Copy(sourcePath, destinationPath);

        var destinationInfo = new FileInfo(destinationPath);
        var now = DateTime.UtcNow;
        var duplicate = new PhotoAsset
        {
            ScanRootId = asset.ScanRootId,
            FullPath = destinationPath,
            FileName = destinationInfo.Name,
            Extension = destinationInfo.Extension.ToLowerInvariant(),
            FolderPath = destinationInfo.DirectoryName ?? string.Empty,
            FileSizeBytes = destinationInfo.Length,
            DateTaken = asset.DateTaken,
            DateTakenSource = asset.DateTakenSource,
            CameraMake = asset.CameraMake,
            CameraModel = asset.CameraModel,
            Width = asset.Width,
            Height = asset.Height,
            Sha256 = asset.Sha256,
            FileCreatedUtc = destinationInfo.CreationTimeUtc,
            FileLastWriteUtc = destinationInfo.LastWriteTimeUtc,
            IndexedUtc = now,
            UpdatedUtc = now,
            Notes = asset.Notes,
            TagsCsv = asset.TagsCsv,
            PeopleCsv = asset.PeopleCsv,
            AnimalsCsv = asset.AnimalsCsv
        };

        db.PhotoAssets.Add(duplicate);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return duplicate;
    }

    public async Task<PhotoAsset?> RepairPhotoLocationAsync(int photoId, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .Include(x => x.ScanRoot)
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        if (File.Exists(asset.FullPath))
        {
            return asset;
        }

        var rootPath = asset.ScanRoot?.RootPath;
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return null;
        }

        var candidatePaths = EnumerateByFileName(rootPath, asset.FileName)
            .Take(500)
            .ToList();
        if (candidatePaths.Count == 0)
        {
            return null;
        }

        var narrowed = candidatePaths;
        if (asset.FileSizeBytes > 0)
        {
            narrowed = narrowed
                .Where(path =>
                {
                    try
                    {
                        return new FileInfo(path).Length == asset.FileSizeBytes;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToList();

            if (narrowed.Count == 0)
            {
                narrowed = candidatePaths;
            }
        }

        if (narrowed.Count > 1 && !string.IsNullOrWhiteSpace(asset.Sha256))
        {
            var hashMatches = new List<string>();
            foreach (var path in narrowed)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var hash = Hashing.ComputeSha256ForFile(path);
                    if (string.Equals(hash, asset.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        hashMatches.Add(path);
                    }
                }
                catch
                {
                    // Skip unreadable candidates.
                }
            }

            if (hashMatches.Count > 0)
            {
                narrowed = hashMatches;
            }
        }

        if (narrowed.Count != 1)
        {
            return null;
        }

        ApplyPathState(asset, narrowed[0], refreshFromFileIfExists: true);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return asset;
    }

    public async Task<PhotoAsset?> UpdatePathReferenceAsync(int photoId, string fullPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            throw new ArgumentException("Path is required.", nameof(fullPath));
        }

        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        ApplyPathState(asset, fullPath, refreshFromFileIfExists: true);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return asset;
    }

    public async Task<PhotoAsset?> UpdateDetectedSubjectsAsync(
        int photoId,
        IReadOnlyList<string> peopleIds,
        IReadOnlyList<string> animalIds,
        CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        asset.PeopleCsv = SerializeIds(peopleIds);
        asset.AnimalsCsv = SerializeIds(animalIds);
        asset.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        WindowsFileMetadataWriter.TryWriteIdentityAndTags(
            asset.FullPath,
            ParseCsvValues(asset.PeopleCsv),
            ParseCsvValues(asset.AnimalsCsv),
            ParseCsvValues(asset.TagsCsv));
        return asset;
    }

    public async Task<bool> DeletePhotoAsync(int photoId, bool deleteFile, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return false;
        }

        if (deleteFile)
        {
            try
            {
                if (File.Exists(asset.FullPath))
                {
                    File.Delete(asset.FullPath);
                }
            }
            catch
            {
                // Best effort delete; record is still removed.
            }
        }

        db.PhotoAssets.Remove(asset);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static async Task<PhotoAsset> RelocatePhotoInternalAsync(
        PhotoCatalogDbContext db,
        PhotoAsset asset,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetFullPath(asset.FullPath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Source file was not found.", sourcePath);
        }

        destinationPath = Path.GetFullPath(destinationPath);
        EnsureInsideRoot(destinationPath, asset.ScanRoot?.RootPath);

        if (PathsEqual(sourcePath, destinationPath))
        {
            return asset;
        }

        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new InvalidOperationException("Destination path is invalid.");
        }

        Directory.CreateDirectory(destinationDirectory);
        destinationPath = ResolveUniquePath(destinationPath, sourcePath);
        File.Move(sourcePath, destinationPath);

        ApplyPathState(asset, destinationPath, refreshFromFileIfExists: true);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return asset;
    }

    private static IEnumerable<string> EnumerateByFileName(string rootPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(fileName))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(Path.GetFullPath(rootPath));

        while (pending.Count > 0)
        {
            var current = pending.Pop();
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
                pending.Push(directory);
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(current, fileName);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }
        }
    }

    private static void ApplyPathState(PhotoAsset asset, string fullPath, bool refreshFromFileIfExists)
    {
        var normalizedPath = Path.GetFullPath(fullPath);
        asset.FullPath = normalizedPath;
        asset.FileName = Path.GetFileName(normalizedPath);
        asset.Extension = NormalizeExtension(Path.GetExtension(normalizedPath));
        asset.FolderPath = Path.GetDirectoryName(normalizedPath) ?? string.Empty;

        if (refreshFromFileIfExists && File.Exists(normalizedPath))
        {
            var info = new FileInfo(normalizedPath);
            asset.FileSizeBytes = info.Length;
            asset.FileCreatedUtc = info.CreationTimeUtc;
            asset.FileLastWriteUtc = info.LastWriteTimeUtc;
        }

        asset.UpdatedUtc = DateTime.UtcNow;
    }

    private static void EnsureInsideRoot(string path, string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return;
        }

        var root = EnsureTrailingSeparator(Path.GetFullPath(rootPath));
        var candidate = Path.GetFullPath(path);
        var candidateWithSeparator = File.Exists(candidate) || candidate.EndsWith(Path.DirectorySeparatorChar)
            ? candidate
            : EnsureTrailingSeparator(candidate);

        if (!candidateWithSeparator.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Destination must stay inside the selected scan root.");
        }
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : $"{path}{Path.DirectorySeparatorChar}";
    }

    private static string ResolveUniquePath(string path, string? ignoreExistingPath = null)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        var candidate = path;
        var suffix = 1;
        while (File.Exists(candidate) &&
               (string.IsNullOrWhiteSpace(ignoreExistingPath) || !PathsEqual(candidate, ignoreExistingPath)))
        {
            candidate = Path.Combine(directory, $"{baseName}_{suffix}{extension}");
            suffix++;
        }

        return candidate;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        return extension.StartsWith('.')
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";
    }

    private static string NormalizeFileName(string fileNameWithoutExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(fileNameWithoutExtension
            .Trim()
            .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
            .ToArray());
        return cleaned.Trim();
    }

    private static string? SerializeIds(IEnumerable<string> ids)
    {
        var normalized = ids
            .Select(NormalizeIdentifier)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();

        return normalized.Count == 0
            ? null
            : string.Join(',', normalized);
    }

    private static string NormalizeIdentifier(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return string.Empty;
        }

        var compact = string.Join('_', id
            .Split(new[] { ' ', '\t', '\r', '\n', ',', ';', ':', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return compact.Trim();
    }

    private static IReadOnlyList<string> ParseCsvValues(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<string>();
        }

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);
    }
}
