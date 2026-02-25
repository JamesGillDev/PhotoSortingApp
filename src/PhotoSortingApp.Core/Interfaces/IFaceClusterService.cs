using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface IFaceClusterService
{
    Task<IReadOnlyList<FaceCluster>> GetClustersAsync(int scanRootId, CancellationToken cancellationToken = default);
}
