using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface ITaggingService
{
    Task<IReadOnlyList<string>> GetTagsAsync(PhotoAsset photo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetTagsAsync(int photoId, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> ReplaceTagsAsync(int photoId, IReadOnlyList<string> tags, CancellationToken cancellationToken = default);
}
