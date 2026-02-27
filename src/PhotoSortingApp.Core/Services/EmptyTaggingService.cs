using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Services;

public class EmptyTaggingService : ITaggingService
{
    public Task<IReadOnlyList<string>> GetTagsAsync(PhotoAsset photo, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> empty = Array.Empty<string>();
        return Task.FromResult(empty);
    }

    public Task<IReadOnlyList<string>> GetTagsAsync(int photoId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> empty = Array.Empty<string>();
        return Task.FromResult(empty);
    }

    public Task<PhotoAsset?> ReplaceTagsAsync(int photoId, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PhotoAsset?>(null);
    }
}
