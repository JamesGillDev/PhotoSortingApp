using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Services;

public class EmptyFaceClusterService : IFaceClusterService
{
    public Task<IReadOnlyList<FaceCluster>> GetClustersAsync(int scanRootId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FaceCluster> empty = Array.Empty<FaceCluster>();
        return Task.FromResult(empty);
    }
}
