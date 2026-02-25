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
}
