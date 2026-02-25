using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface IMetadataService
{
    Task<PhotoMetadataResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}
