using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface IPhotoQueryService
{
    Task<PhotoQueryResult> QueryPhotosAsync(PhotoQueryFilter filter, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> GetPhotoByIdAsync(int photoId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SmartAlbumItem>> GetSmartAlbumsAsync(int? scanRootId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetFolderSubpathsAsync(int scanRootId, CancellationToken cancellationToken = default);
}
