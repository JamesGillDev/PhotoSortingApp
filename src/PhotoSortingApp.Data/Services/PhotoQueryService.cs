using Microsoft.EntityFrameworkCore;
using PhotoSortingApp.Core.Infrastructure;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Enums;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Data.Services;

public class PhotoQueryService : IPhotoQueryService
{
    private const string YearPrefix = "year:";
    private const string MonthPrefix = "month:";
    private const string DuplicatePrefix = "dup:";
    private const string NoCaseCollation = "NOCASE";
    private const string LikeEscape = "\\";
    private static readonly string[] ExcludedFolderLikePatterns =
    {
        "%\\Windows\\%",
        "%\\Windows",
        "%\\Program Files\\%",
        "%\\Program Files (x86)\\%",
        "%\\ProgramData\\%",
        "%\\AppData\\%",
        "%\\$Recycle.Bin\\%",
        "%\\System Volume Information\\%",
        "%\\.git\\%",
        "%\\.git",
        "%\\.nuget\\%",
        "%\\.nuget",
        "%\\.vs\\%",
        "%\\.vs",
        "%\\.vscode\\%",
        "%\\.vscode",
        "%\\node_modules\\%",
        "%\\node_modules",
        "%\\bin\\%",
        "%\\bin",
        "%\\obj\\%",
        "%\\obj",
        "%\\publish\\%",
        "%\\publish",
        "%\\artifacts\\%",
        "%\\artifacts",
        "%\\packages\\%",
        "%\\packages",
        "%\\packagecache\\%",
        "%\\packagecache",
        "%\\bower_components\\%",
        "%\\bower_components",
        "%\\venv\\%",
        "%\\venv",
        "%\\.venv\\%",
        "%\\.venv",
        "%\\__pycache__\\%",
        "%\\__pycache__"
    };
    private static readonly string[] GeneratedAssetNameLikePatterns =
    {
        "%appicon%",
        "%favicon%",
        "%targetsize%",
        "%.scale-%",
        "%storelogo%",
        "%splashscreen%",
        "%square44x44%",
        "%square150x150%",
        "%windowsappruntime%"
    };
    private static readonly string[] ImageExtensions = SupportedPhotoExtensions.Images
        .Select(x => x.ToLowerInvariant())
        .ToArray();
    private static readonly string[] VideoExtensions = SupportedPhotoExtensions.Videos
        .Select(x => x.ToLowerInvariant())
        .ToArray();

    private readonly Func<PhotoCatalogDbContext> _contextFactory;

    public PhotoQueryService(Func<PhotoCatalogDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PhotoQueryResult> QueryPhotosAsync(PhotoQueryFilter filter, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var query = db.PhotoAssets.AsNoTracking().AsQueryable();

        if (filter.ScanRootId.HasValue)
        {
            query = query.Where(x => x.ScanRootId == filter.ScanRootId.Value);
        }

        if (filter.ExcludeSystemFolders)
        {
            query = ApplySystemPathExclusion(query);
        }

        if (!filter.IncludeImages || !filter.IncludeVideos)
        {
            if (filter.IncludeImages && !filter.IncludeVideos)
            {
                query = query.Where(x => ImageExtensions.Contains(x.Extension));
            }
            else if (!filter.IncludeImages && filter.IncludeVideos)
            {
                query = query.Where(x => VideoExtensions.Contains(x.Extension));
            }
            else
            {
                query = query.Where(_ => false);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var searchTokens = ParseSearchTokens(filter.SearchText);
            foreach (var token in searchTokens)
            {
                var pattern = BuildContainsLikePattern(token);
                query = query.Where(x =>
                    EF.Functions.Like(EF.Functions.Collate(x.FileName, NoCaseCollation), pattern, LikeEscape) ||
                    (x.Notes != null && EF.Functions.Like(EF.Functions.Collate(x.Notes, NoCaseCollation), pattern, LikeEscape)) ||
                    (x.TagsCsv != null && EF.Functions.Like(EF.Functions.Collate(x.TagsCsv, NoCaseCollation), pattern, LikeEscape)) ||
                    (x.PeopleCsv != null && EF.Functions.Like(EF.Functions.Collate(x.PeopleCsv, NoCaseCollation), pattern, LikeEscape)) ||
                    (x.AnimalsCsv != null && EF.Functions.Like(EF.Functions.Collate(x.AnimalsCsv, NoCaseCollation), pattern, LikeEscape)) ||
                    (x.LocationsCsv != null && EF.Functions.Like(EF.Functions.Collate(x.LocationsCsv, NoCaseCollation), pattern, LikeEscape)));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.PersonSearchText))
        {
            var personTokens = ParseSearchTokens(filter.PersonSearchText);
            foreach (var token in personTokens)
            {
                var pattern = BuildContainsLikePattern(token);
                query = query.Where(x =>
                    x.PeopleCsv != null &&
                    EF.Functions.Like(EF.Functions.Collate(x.PeopleCsv, NoCaseCollation), pattern, LikeEscape));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.AnimalSearchText))
        {
            var animalTokens = ParseSearchTokens(filter.AnimalSearchText);
            foreach (var token in animalTokens)
            {
                var pattern = BuildContainsLikePattern(token);
                query = query.Where(x =>
                    x.AnimalsCsv != null &&
                    EF.Functions.Like(EF.Functions.Collate(x.AnimalsCsv, NoCaseCollation), pattern, LikeEscape));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.LocationSearchText))
        {
            var locationTokens = ParseSearchTokens(filter.LocationSearchText);
            foreach (var token in locationTokens)
            {
                var pattern = BuildContainsLikePattern(token);
                query = query.Where(x =>
                    x.LocationsCsv != null &&
                    EF.Functions.Like(EF.Functions.Collate(x.LocationsCsv, NoCaseCollation), pattern, LikeEscape));
            }
        }

        if (filter.FromDateUtc.HasValue)
        {
            query = query.Where(x => x.DateTaken != null && x.DateTaken >= filter.FromDateUtc.Value);
        }

        if (filter.ToDateUtc.HasValue)
        {
            query = query.Where(x => x.DateTaken != null && x.DateTaken <= filter.ToDateUtc.Value);
        }

        if (filter.DateSource.HasValue)
        {
            query = query.Where(x => x.DateTakenSource == filter.DateSource.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.FolderSubpath) && filter.ScanRootId.HasValue)
        {
            var rootPath = await db.ScanRoots
                .Where(x => x.Id == filter.ScanRootId.Value)
                .Select(x => x.RootPath)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                var absoluteSubpath = Path.GetFullPath(Path.Combine(rootPath, filter.FolderSubpath));
                query = query.Where(x => x.FolderPath.StartsWith(absoluteSubpath));
            }
        }

        query = ApplyAlbumFilter(query, db, filter.AlbumKey, filter.ScanRootId);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 20, 500);

        query = ApplySort(query, filter.SortBy);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PhotoQueryResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<PhotoAsset?> GetPhotoByIdAsync(int photoId, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        return await db.PhotoAssets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SmartAlbumItem>> GetSmartAlbumsAsync(
        int? scanRootId,
        bool excludeSystemFolders = false,
        CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var baseQuery = db.PhotoAssets.AsNoTracking().AsQueryable();

        if (scanRootId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.ScanRootId == scanRootId.Value);
        }

        if (excludeSystemFolders)
        {
            baseQuery = ApplySystemPathExclusion(baseQuery);
        }

        var albums = new List<SmartAlbumItem>();
        var total = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        albums.Add(new SmartAlbumItem { Key = "all", Name = "All Media", Count = total });

        var unknownCount = await baseQuery.CountAsync(
            x => x.DateTaken == null || x.DateTakenSource == DateTakenSource.Unknown,
            cancellationToken).ConfigureAwait(false);
        albums.Add(new SmartAlbumItem { Key = "unknown", Name = "Unknown Date", Count = unknownCount });

        var recentThreshold = DateTime.UtcNow.AddDays(-30);
        var recentCount = await baseQuery.CountAsync(x => x.IndexedUtc >= recentThreshold, cancellationToken).ConfigureAwait(false);
        albums.Add(new SmartAlbumItem { Key = "recent", Name = "Recently Added", Count = recentCount });

        var videosCount = await baseQuery
            .CountAsync(x => VideoExtensions.Contains(x.Extension), cancellationToken)
            .ConfigureAwait(false);
        albums.Add(new SmartAlbumItem { Key = "videos", Name = "Videos", Count = videosCount });

        var monthGroups = await baseQuery
            .Where(x => x.DateTaken != null)
            .GroupBy(x => new { x.DateTaken!.Value.Year, x.DateTaken.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var group in monthGroups)
        {
            albums.Add(new SmartAlbumItem
            {
                Key = $"{MonthPrefix}{group.Year:D4}-{group.Month:D2}",
                Name = $"{group.Year:D4}-{group.Month:D2}",
                Count = group.Count
            });
        }

        var yearGroups = await baseQuery
            .Where(x => x.DateTaken != null)
            .GroupBy(x => x.DateTaken!.Value.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Year)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var group in yearGroups)
        {
            albums.Add(new SmartAlbumItem
            {
                Key = $"{YearPrefix}{group.Year:D4}",
                Name = $"Year {group.Year:D4}",
                Count = group.Count
            });
        }

        var duplicateHashes = baseQuery
            .Where(x => x.Sha256 != null && x.Sha256 != string.Empty)
            .GroupBy(x => x.Sha256)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key!);

        var duplicatesCount = await baseQuery
            .Where(x => x.Sha256 != null && duplicateHashes.Contains(x.Sha256))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        albums.Add(new SmartAlbumItem { Key = "duplicates", Name = "Possible Duplicates", Count = duplicatesCount });

        return albums;
    }

    public async Task<IReadOnlyList<string>> GetFolderSubpathsAsync(
        int scanRootId,
        bool excludeSystemFolders = false,
        CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var root = await db.ScanRoots.AsNoTracking()
            .SingleAsync(x => x.Id == scanRootId, cancellationToken)
            .ConfigureAwait(false);

        var foldersQuery = db.PhotoAssets.AsNoTracking()
            .Where(x => x.ScanRootId == scanRootId);

        if (excludeSystemFolders)
        {
            foldersQuery = ApplySystemPathExclusion(foldersQuery);
        }

        var folders = await foldersQuery
            .Select(x => x.FolderPath)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var relative = folders
            .Select(path => Path.GetRelativePath(root.RootPath, path))
            .Select(path => string.IsNullOrWhiteSpace(path) ? "." : path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return relative;
    }

    private static IReadOnlyList<string> ParseSearchTokens(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return Array.Empty<string>();
        }

        var tokens = rawInput
            .Trim()
            .Split(
                new[] { ' ', '\t', '\r', '\n', ',', ';', '|', '/', '\\', '-', '_', '.' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        if (tokens.Count == 0)
        {
            tokens.Add(rawInput.Trim());
        }

        return tokens;
    }

    private static string BuildContainsLikePattern(string token)
    {
        var escaped = token
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    private static IQueryable<PhotoAsset> ApplyAlbumFilter(
        IQueryable<PhotoAsset> query,
        PhotoCatalogDbContext db,
        string? albumKey,
        int? scanRootId)
    {
        if (string.IsNullOrWhiteSpace(albumKey) || albumKey.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return query;
        }

        if (albumKey.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return query.Where(x => x.DateTaken == null || x.DateTakenSource == DateTakenSource.Unknown);
        }

        if (albumKey.Equals("recent", StringComparison.OrdinalIgnoreCase))
        {
            var recentThreshold = DateTime.UtcNow.AddDays(-30);
            return query.Where(x => x.IndexedUtc >= recentThreshold);
        }

        if (albumKey.Equals("duplicates", StringComparison.OrdinalIgnoreCase))
        {
            var dupBase = db.PhotoAssets.AsNoTracking().AsQueryable();
            if (scanRootId.HasValue)
            {
                dupBase = dupBase.Where(x => x.ScanRootId == scanRootId.Value);
            }

            var duplicateHashes = dupBase
                .Where(x => x.Sha256 != null && x.Sha256 != string.Empty)
                .GroupBy(x => x.Sha256)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key!);

            return query.Where(x => x.Sha256 != null && duplicateHashes.Contains(x.Sha256));
        }

        if (albumKey.Equals("videos", StringComparison.OrdinalIgnoreCase))
        {
            return query.Where(x => VideoExtensions.Contains(x.Extension));
        }

        if (albumKey.StartsWith(DuplicatePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var hash = albumKey[DuplicatePrefix.Length..];
            if (!string.IsNullOrWhiteSpace(hash))
            {
                return query.Where(x => x.Sha256 == hash);
            }
        }

        if (albumKey.StartsWith(YearPrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(albumKey[YearPrefix.Length..], out var year))
        {
            return query.Where(x => x.DateTaken != null && x.DateTaken.Value.Year == year);
        }

        if (albumKey.StartsWith(MonthPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var raw = albumKey[MonthPrefix.Length..];
            var split = raw.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 2 &&
                int.TryParse(split[0], out var monthYear) &&
                int.TryParse(split[1], out var month))
            {
                return query.Where(x =>
                    x.DateTaken != null &&
                    x.DateTaken.Value.Year == monthYear &&
                    x.DateTaken.Value.Month == month);
            }
        }

        return query;
    }

    private static IQueryable<PhotoAsset> ApplySort(IQueryable<PhotoAsset> query, PhotoSortOption sortBy)
    {
        return sortBy switch
        {
            PhotoSortOption.DateTakenOldest => query
                .OrderBy(x => x.DateTaken == null)
                .ThenBy(x => x.DateTaken)
                .ThenBy(x => x.FileName),
            PhotoSortOption.DateAddedNewest => query
                .OrderByDescending(x => x.IndexedUtc)
                .ThenBy(x => x.FileName),
            PhotoSortOption.DateAddedOldest => query
                .OrderBy(x => x.IndexedUtc)
                .ThenBy(x => x.FileName),
            PhotoSortOption.FileSizeLargest => query
                .OrderByDescending(x => x.FileSizeBytes)
                .ThenBy(x => x.FileName),
            PhotoSortOption.FileSizeSmallest => query
                .OrderBy(x => x.FileSizeBytes)
                .ThenBy(x => x.FileName),
            PhotoSortOption.NameAscending => query
                .OrderBy(x => x.FileName)
                .ThenByDescending(x => x.IndexedUtc),
            PhotoSortOption.NameDescending => query
                .OrderByDescending(x => x.FileName)
                .ThenByDescending(x => x.IndexedUtc),
            _ => query
                .OrderByDescending(x => x.DateTaken != null)
                .ThenByDescending(x => x.DateTaken)
                .ThenByDescending(x => x.IndexedUtc)
                .ThenBy(x => x.FileName)
        };
    }

    private static IQueryable<PhotoAsset> ApplySystemPathExclusion(IQueryable<PhotoAsset> query)
    {
        foreach (var folderPattern in ExcludedFolderLikePatterns)
        {
            var localPattern = folderPattern;
            query = query.Where(x =>
                !EF.Functions.Like(EF.Functions.Collate(x.FolderPath, NoCaseCollation), localPattern));
        }

        foreach (var fileNamePattern in GeneratedAssetNameLikePatterns)
        {
            var localPattern = fileNamePattern;
            query = query.Where(x =>
                !(ImageExtensions.Contains(x.Extension) &&
                  x.Width.HasValue &&
                  x.Height.HasValue &&
                  x.Width.Value <= 512 &&
                  x.Height.Value <= 512 &&
                  x.FileSizeBytes <= 1_500_000 &&
                  EF.Functions.Like(EF.Functions.Collate(x.FileName, NoCaseCollation), localPattern)));
        }

        return query;
    }
}
