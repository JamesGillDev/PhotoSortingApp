using PhotoSortingApp.Core.Interfaces;

namespace PhotoSortingApp.Core.Services;

public class EmptySemanticSearchService : ISemanticSearchService
{
    public Task<IReadOnlyList<int>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<int> empty = Array.Empty<int>();
        return Task.FromResult(empty);
    }
}
