namespace PhotoSortingApp.Core.Interfaces;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<int>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
