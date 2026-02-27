using Microsoft.EntityFrameworkCore;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Data.Services;

public class DatabaseTaggingService : ITaggingService
{
    private readonly Func<PhotoCatalogDbContext> _contextFactory;

    public DatabaseTaggingService(Func<PhotoCatalogDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public Task<IReadOnlyList<string>> GetTagsAsync(PhotoAsset photo, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ParseTags(photo.TagsCsv));
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync(int photoId, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var raw = await db.PhotoAssets.AsNoTracking()
            .Where(x => x.Id == photoId)
            .Select(x => x.TagsCsv)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return ParseTags(raw);
    }

    public async Task<PhotoAsset?> ReplaceTagsAsync(int photoId, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        using var db = _contextFactory();
        var asset = await db.PhotoAssets
            .SingleOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            .ConfigureAwait(false);
        if (asset is null)
        {
            return null;
        }

        asset.TagsCsv = SerializeTags(tags);
        asset.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return asset;
    }

    private static IReadOnlyList<string> ParseTags(string? tagsCsv)
    {
        if (string.IsNullOrWhiteSpace(tagsCsv))
        {
            return Array.Empty<string>();
        }

        return tagsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? SerializeTags(IEnumerable<string> tags)
    {
        var normalized = tags
            .Select(NormalizeTag)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();

        return normalized.Count == 0
            ? null
            : string.Join(',', normalized);
    }

    private static string NormalizeTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        var compact = string.Join(' ', tag.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return compact.Replace(',', ' ').Trim();
    }
}
