using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Core.Services;
using PhotoSortingApp.Data;
using PhotoSortingApp.Data.Services;

namespace PhotoSortingApp.App.Services;

public class AppServices
{
    private readonly Func<PhotoCatalogDbContext> _contextFactory;

    public AppServices(string? baseDirectory = null)
    {
        var options = PhotoCatalogDb.CreateOptions(baseDirectory);
        _contextFactory = () => new PhotoCatalogDbContext(options);

        MetadataService = new MetadataService();
        ThumbnailService = new ThumbnailService(baseDirectory);

        ScanService = new ScanService(_contextFactory, MetadataService);
        PhotoQueryService = new PhotoQueryService(_contextFactory);
        DuplicateService = new DuplicateService(_contextFactory);
        OrganizerPlanService = new OrganizerPlanService(_contextFactory, baseDirectory);

        TaggingService = new EmptyTaggingService();
        SemanticSearchService = new EmptySemanticSearchService();
        FaceClusterService = new EmptyFaceClusterService();
    }

    public IMetadataService MetadataService { get; }

    public IThumbnailService ThumbnailService { get; }

    public IScanService ScanService { get; }

    public IPhotoQueryService PhotoQueryService { get; }

    public IDuplicateService DuplicateService { get; }

    public IOrganizerPlanService OrganizerPlanService { get; }

    public ITaggingService TaggingService { get; }

    public ISemanticSearchService SemanticSearchService { get; }

    public IFaceClusterService FaceClusterService { get; }

    public Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        return DatabaseInitializer.EnsureMigratedAsync(_contextFactory, cancellationToken);
    }
}
