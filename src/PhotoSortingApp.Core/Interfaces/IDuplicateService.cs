using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface IDuplicateService
{
    Task<int> ComputeMissingHashesAsync(int scanRootId, IProgress<ScanProgressInfo>? progress = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DuplicateGroup>> GetDuplicateGroupsAsync(int scanRootId, CancellationToken cancellationToken = default);
}
