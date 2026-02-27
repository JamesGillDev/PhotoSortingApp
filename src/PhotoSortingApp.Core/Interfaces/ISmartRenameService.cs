using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface ISmartRenameService
{
    Task<SmartRenameAnalysis> AnalyzeAsync(
        PhotoAsset photo,
        IReadOnlyList<string> existingTags,
        CancellationToken cancellationToken = default);
}
