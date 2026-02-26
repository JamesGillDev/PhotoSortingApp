using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface IScanService
{
    Task<IReadOnlyList<ScanRoot>> GetScanRootsAsync(CancellationToken cancellationToken = default);

    Task<ScanRoot> GetOrCreateScanRootAsync(string rootPath, bool enableDuplicateDetection, CancellationToken cancellationToken = default);

    Task UpdateDuplicateDetectionAsync(int scanRootId, bool enabled, CancellationToken cancellationToken = default);

    Task<ScanResult> ScanAsync(
        int scanRootId,
        IProgress<ScanProgressInfo>? progress = null,
        CancellationToken cancellationToken = default,
        ScanOptions? options = null);
}
