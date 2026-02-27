using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface IPhotoEditService
{
    Task<PhotoAsset?> GetPhotoByIdAsync(int photoId, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> RenamePhotoAsync(int photoId, string requestedFileName, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> MovePhotoAsync(int photoId, string destinationFolderPath, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> RelocatePhotoAsync(int photoId, string destinationPath, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> CopyPhotoAsync(int photoId, string destinationFolderPath, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> DuplicatePhotoAsync(int photoId, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> RepairPhotoLocationAsync(int photoId, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> UpdatePathReferenceAsync(int photoId, string fullPath, CancellationToken cancellationToken = default);

    Task<PhotoAsset?> UpdateDetectedSubjectsAsync(
        int photoId,
        IReadOnlyList<string> peopleIds,
        IReadOnlyList<string> animalIds,
        CancellationToken cancellationToken = default);

    Task<bool> DeletePhotoAsync(int photoId, bool deleteFile, CancellationToken cancellationToken = default);
}
