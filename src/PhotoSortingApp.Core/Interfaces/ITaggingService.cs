using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface ITaggingService
{
    Task<IReadOnlyList<string>> GetTagsAsync(PhotoAsset photo, CancellationToken cancellationToken = default);
}
