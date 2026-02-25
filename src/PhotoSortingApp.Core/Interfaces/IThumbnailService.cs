using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface IThumbnailService
{
    Task<string?> GetThumbnailPathAsync(PhotoAsset asset, int maxPixelSize = 256, CancellationToken cancellationToken = default);

    void Invalidate(PhotoAsset asset);
}
