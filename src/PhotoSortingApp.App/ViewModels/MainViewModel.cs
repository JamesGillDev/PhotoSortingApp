using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PhotoSortingApp.App.Services;
using PhotoSortingApp.App.Theming;
using PhotoSortingApp.App.Utils;
using PhotoSortingApp.Domain.Enums;
using PhotoSortingApp.Domain.Models;
using WinForms = System.Windows.Forms;

namespace PhotoSortingApp.App.ViewModels;

public class MainViewModel : ObservableObject
{
    private const int PageSize = 120;

    private readonly AppServices _services;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _queryCts;
    private CancellationTokenSource? _previewCts;
    private CancellationTokenSource? _thumbnailCts;
    private bool _isReady;
    private bool _suppressDuplicateUpdate;
    private bool _isLoadingPhotos;
    private int _nextPage = 1;
    private int _loadedCount;
    private List<OrganizerPlanItem> _latestOrganizerPlanItems = new();

    private ScanRootItemViewModel? _selectedScanRoot;
    private SmartAlbumItemViewModel? _selectedAlbum;
    private FolderFilterItemViewModel? _selectedFolderFilter;
    private DateSourceOptionViewModel? _selectedDateSource;
    private SortOptionViewModel? _selectedSortOption;
    private LayoutOptionViewModel? _selectedLayoutOption;
    private ThemeOptionViewModel? _selectedThemeOption;
    private DuplicateGroupItemViewModel? _selectedDuplicateGroup;
    private PhotoItemViewModel? _selectedPhoto;
    private BitmapImage? _previewImage;
    private DateTime? _fromDateLocal;
    private DateTime? _toDateLocal;
    private string _searchText = string.Empty;
    private string _statusMessage = "Select a folder to begin indexing.";
    private string _indexingStatus = string.Empty;
    private string _planSummary = "No organizer plan generated yet.";
    private bool _isIndexing;
    private bool _enableDuplicateDetection;
    private bool _hasMoreResults;
    private int _totalResults;
    private double _indexingPercent;

    public MainViewModel(AppServices services)
    {
        _services = services;

        ScanRoots = new ObservableCollection<ScanRootItemViewModel>();
        SmartAlbums = new ObservableCollection<SmartAlbumItemViewModel>();
        FolderFilters = new ObservableCollection<FolderFilterItemViewModel>();
        DuplicateGroups = new ObservableCollection<DuplicateGroupItemViewModel>();
        Photos = new ObservableCollection<PhotoItemViewModel>();
        OrganizerPreviewItems = new ObservableCollection<OrganizerPlanItem>();

        DateSourceOptions = new ObservableCollection<DateSourceOptionViewModel>
        {
            new() { DisplayName = "All Date Sources", Value = null },
            new() { DisplayName = "EXIF", Value = DateTakenSource.Exif },
            new() { DisplayName = "File Created", Value = DateTakenSource.FileCreated },
            new() { DisplayName = "File Modified", Value = DateTakenSource.FileModified },
            new() { DisplayName = "Unknown", Value = DateTakenSource.Unknown }
        };
        SelectedDateSource = DateSourceOptions[0];
        SortOptions = new ObservableCollection<SortOptionViewModel>
        {
            new() { DisplayName = "Date Taken (Newest)", Value = PhotoSortOption.DateTakenNewest },
            new() { DisplayName = "Date Taken (Oldest)", Value = PhotoSortOption.DateTakenOldest },
            new() { DisplayName = "Date Indexed (Newest)", Value = PhotoSortOption.DateAddedNewest },
            new() { DisplayName = "Date Indexed (Oldest)", Value = PhotoSortOption.DateAddedOldest },
            new() { DisplayName = "File Size (Largest)", Value = PhotoSortOption.FileSizeLargest },
            new() { DisplayName = "File Size (Smallest)", Value = PhotoSortOption.FileSizeSmallest },
            new() { DisplayName = "Name (A-Z)", Value = PhotoSortOption.NameAscending },
            new() { DisplayName = "Name (Z-A)", Value = PhotoSortOption.NameDescending }
        };
        SelectedSortOption = SortOptions[0];

        LayoutOptions = new ObservableCollection<LayoutOptionViewModel>
        {
            new() { DisplayName = "Tiles", Value = GalleryLayoutMode.Tiles },
            new() { DisplayName = "List", Value = GalleryLayoutMode.List }
        };
        SelectedLayoutOption = LayoutOptions[0];

        ThemeOptions = new ObservableCollection<ThemeOptionViewModel>
        {
            new() { DisplayName = "System", Value = AppThemePreference.System },
            new() { DisplayName = "Light", Value = AppThemePreference.Light },
            new() { DisplayName = "Dark", Value = AppThemePreference.Dark }
        };
        SelectedThemeOption = ThemeOptions[0];

        SelectFolderCommand = new AsyncRelayCommand(SelectFolderAsync);
        RefreshScanRootsCommand = new AsyncRelayCommand(RefreshScanRootsAsync);
        ScanWholeComputerCommand = new AsyncRelayCommand(ScanWholeComputerAsync, () => !IsIndexing);
        RescanCommand = new AsyncRelayCommand(RescanAsync, () => SelectedScanRoot is not null && !IsIndexing);
        CancelScanCommand = new RelayCommand(CancelScan, () => IsIndexing);
        ApplyFiltersCommand = new AsyncRelayCommand(() => LoadPhotosAsync(reset: true), () => SelectedScanRoot is not null);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => SelectedScanRoot is not null);
        LoadMoreCommand = new AsyncRelayCommand(() => LoadPhotosAsync(reset: false), () => SelectedScanRoot is not null && HasMoreResults);
        OpenFileLocationCommand = new RelayCommand(OpenFileLocation, () => SelectedPhoto is not null);
        BuildOrganizerPlanCommand = new AsyncRelayCommand(BuildOrganizerPlanAsync, () => SelectedScanRoot is not null);
        ApplyOrganizerPlanCommand = new AsyncRelayCommand(ApplyOrganizerPlanAsync, () => SelectedScanRoot is not null && _latestOrganizerPlanItems.Count > 0 && !IsIndexing);
        RefreshDuplicatesCommand = new AsyncRelayCommand(RefreshDuplicatesAsync, () => SelectedScanRoot is not null && EnableDuplicateDetection);
    }

    public ObservableCollection<ScanRootItemViewModel> ScanRoots { get; }

    public ObservableCollection<SmartAlbumItemViewModel> SmartAlbums { get; }

    public ObservableCollection<FolderFilterItemViewModel> FolderFilters { get; }

    public ObservableCollection<DateSourceOptionViewModel> DateSourceOptions { get; }

    public ObservableCollection<SortOptionViewModel> SortOptions { get; }

    public ObservableCollection<LayoutOptionViewModel> LayoutOptions { get; }

    public ObservableCollection<ThemeOptionViewModel> ThemeOptions { get; }

    public ObservableCollection<DuplicateGroupItemViewModel> DuplicateGroups { get; }

    public ObservableCollection<PhotoItemViewModel> Photos { get; }

    public ObservableCollection<OrganizerPlanItem> OrganizerPreviewItems { get; }

    public ICommand SelectFolderCommand { get; }

    public ICommand RefreshScanRootsCommand { get; }

    public ICommand ScanWholeComputerCommand { get; }

    public ICommand RescanCommand { get; }

    public ICommand CancelScanCommand { get; }

    public ICommand ApplyFiltersCommand { get; }

    public ICommand ClearFiltersCommand { get; }

    public ICommand LoadMoreCommand { get; }

    public ICommand OpenFileLocationCommand { get; }

    public ICommand BuildOrganizerPlanCommand { get; }

    public ICommand ApplyOrganizerPlanCommand { get; }

    public ICommand RefreshDuplicatesCommand { get; }

    public ScanRootItemViewModel? SelectedScanRoot
    {
        get => _selectedScanRoot;
        set
        {
            if (!SetProperty(ref _selectedScanRoot, value))
            {
                return;
            }

            _ = OnSelectedScanRootChangedAsync();
            RaiseCommandCanExecute();
        }
    }

    public SmartAlbumItemViewModel? SelectedAlbum
    {
        get => _selectedAlbum;
        set
        {
            if (!SetProperty(ref _selectedAlbum, value))
            {
                return;
            }

            if (_isReady)
            {
                _ = LoadPhotosAsync(reset: true);
            }
        }
    }

    public FolderFilterItemViewModel? SelectedFolderFilter
    {
        get => _selectedFolderFilter;
        set => SetProperty(ref _selectedFolderFilter, value);
    }

    public DateSourceOptionViewModel? SelectedDateSource
    {
        get => _selectedDateSource;
        set => SetProperty(ref _selectedDateSource, value);
    }

    public SortOptionViewModel? SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (!SetProperty(ref _selectedSortOption, value))
            {
                return;
            }

            if (_isReady && SelectedScanRoot is not null)
            {
                _ = LoadPhotosAsync(reset: true);
            }
        }
    }

    public LayoutOptionViewModel? SelectedLayoutOption
    {
        get => _selectedLayoutOption;
        set
        {
            if (!SetProperty(ref _selectedLayoutOption, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(IsTileLayout));
        }
    }

    public ThemeOptionViewModel? SelectedThemeOption
    {
        get => _selectedThemeOption;
        set
        {
            if (!SetProperty(ref _selectedThemeOption, value))
            {
                return;
            }

            ApplyThemePreference(value?.Value ?? AppThemePreference.System);
        }
    }

    public DuplicateGroupItemViewModel? SelectedDuplicateGroup
    {
        get => _selectedDuplicateGroup;
        set
        {
            if (!SetProperty(ref _selectedDuplicateGroup, value))
            {
                return;
            }

            if (_isReady && value is not null)
            {
                _ = LoadPhotosAsync(reset: true);
            }
        }
    }

    public PhotoItemViewModel? SelectedPhoto
    {
        get => _selectedPhoto;
        set
        {
            if (!SetProperty(ref _selectedPhoto, value))
            {
                return;
            }

            _ = LoadPreviewAsync(value);
            RaiseCommandCanExecute();
        }
    }

    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        private set => SetProperty(ref _previewImage, value);
    }

    public DateTime? FromDateLocal
    {
        get => _fromDateLocal;
        set => SetProperty(ref _fromDateLocal, value);
    }

    public DateTime? ToDateLocal
    {
        get => _toDateLocal;
        set => SetProperty(ref _toDateLocal, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string IndexingStatus
    {
        get => _indexingStatus;
        private set => SetProperty(ref _indexingStatus, value);
    }

    public string PlanSummary
    {
        get => _planSummary;
        private set => SetProperty(ref _planSummary, value);
    }

    public bool IsIndexing
    {
        get => _isIndexing;
        private set
        {
            if (!SetProperty(ref _isIndexing, value))
            {
                return;
            }

            RaiseCommandCanExecute();
        }
    }

    public bool EnableDuplicateDetection
    {
        get => _enableDuplicateDetection;
        set
        {
            if (!SetProperty(ref _enableDuplicateDetection, value))
            {
                return;
            }

            if (_suppressDuplicateUpdate || SelectedScanRoot is null)
            {
                return;
            }

            _ = UpdateDuplicateDetectionAsync(value);
        }
    }

    public bool HasMoreResults
    {
        get => _hasMoreResults;
        private set
        {
            if (!SetProperty(ref _hasMoreResults, value))
            {
                return;
            }

            RaiseCommandCanExecute();
        }
    }

    public int TotalResults
    {
        get => _totalResults;
        private set => SetProperty(ref _totalResults, value);
    }

    public double IndexingPercent
    {
        get => _indexingPercent;
        private set => SetProperty(ref _indexingPercent, value);
    }

    public string ResultsSummary => $"{Photos.Count} loaded / {TotalResults} matched";

    public bool IsTileLayout => SelectedLayoutOption?.Value != GalleryLayoutMode.List;

    public async Task InitializeAsync()
    {
        await RefreshScanRootsAsync().ConfigureAwait(true);
        _isReady = true;
    }

    private async Task OnSelectedScanRootChangedAsync()
    {
        if (SelectedScanRoot is null)
        {
            Photos.Clear();
            SmartAlbums.Clear();
            FolderFilters.Clear();
            DuplicateGroups.Clear();
            OrganizerPreviewItems.Clear();
            _latestOrganizerPlanItems.Clear();
            SelectedPhoto = null;
            PreviewImage = null;
            PlanSummary = "No organizer plan generated yet.";
            StatusMessage = "Select a folder to begin indexing.";
            RaisePropertyChanged(nameof(ResultsSummary));
            RaiseCommandCanExecute();
            return;
        }

        _suppressDuplicateUpdate = true;
        EnableDuplicateDetection = SelectedScanRoot.EnableDuplicateDetection;
        _suppressDuplicateUpdate = false;
        OrganizerPreviewItems.Clear();
        _latestOrganizerPlanItems.Clear();
        PlanSummary = "No organizer plan generated yet.";

        await RefreshFiltersAndAlbumsAsync().ConfigureAwait(true);
        await LoadPhotosAsync(reset: true).ConfigureAwait(true);
        RaiseCommandCanExecute();
    }

    private async Task RefreshScanRootsAsync()
    {
        var currentSelectionId = SelectedScanRoot?.Id;
        var roots = await _services.ScanService.GetScanRootsAsync().ConfigureAwait(true);

        ScanRoots.Clear();
        foreach (var root in roots)
        {
            ScanRoots.Add(new ScanRootItemViewModel
            {
                Id = root.Id,
                RootPath = root.RootPath,
                EnableDuplicateDetection = root.EnableDuplicateDetection,
                LastScanUtc = root.LastScanUtc,
                TotalFilesLastScan = root.TotalFilesLastScan
            });
        }

        if (ScanRoots.Count == 0)
        {
            SelectedScanRoot = null;
            return;
        }

        SelectedScanRoot = ScanRoots.FirstOrDefault(x => x.Id == currentSelectionId) ?? ScanRoots[0];
    }

    private async Task SelectFolderAsync()
    {
        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "Select a root folder that contains photos",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() != WinForms.DialogResult.OK)
        {
            return;
        }

        var selected = await _services.ScanService
            .GetOrCreateScanRootAsync(dialog.SelectedPath, EnableDuplicateDetection)
            .ConfigureAwait(true);
        await RefreshScanRootsAsync().ConfigureAwait(true);
        SelectedScanRoot = ScanRoots.FirstOrDefault(x => x.Id == selected.Id);
        StatusMessage = $"Selected root: {selected.RootPath}";
    }

    private async Task ScanWholeComputerAsync()
    {
        var fixedDrives = DriveInfo.GetDrives()
            .Where(x => x.IsReady && x.DriveType == DriveType.Fixed)
            .Select(x => x.RootDirectory.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (fixedDrives.Count == 0)
        {
            StatusMessage = "No fixed drives are available for whole-computer scan.";
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();

        IsIndexing = true;
        IndexingPercent = 0;
        IndexingStatus = "Preparing whole-computer scan (safe mode)...";
        StatusMessage = "Whole-computer scan started. System and program folders are excluded.";

        try
        {
            var scanRoots = new List<ScanRoot>();
            foreach (var driveRoot in fixedDrives)
            {
                _scanCts.Token.ThrowIfCancellationRequested();
                var scanRoot = await _services.ScanService
                    .GetOrCreateScanRootAsync(driveRoot, EnableDuplicateDetection, _scanCts.Token)
                    .ConfigureAwait(true);
                scanRoots.Add(scanRoot);
            }

            var aggregate = new ScanResult();
            for (var index = 0; index < scanRoots.Count; index++)
            {
                _scanCts.Token.ThrowIfCancellationRequested();
                var root = scanRoots[index];
                var currentIndex = index;
                var progress = new Progress<ScanProgressInfo>(info =>
                {
                    var processed = info.FilesIndexed + info.FilesUpdated + info.FilesSkipped;
                    var drivePercent = info.FilesFound > 0
                        ? Math.Min(100d, processed * 100d / info.FilesFound)
                        : 0d;
                    IndexingPercent = Math.Min(100d, ((currentIndex + (drivePercent / 100d)) / scanRoots.Count) * 100d);
                    IndexingStatus =
                        $"Drive {root.RootPath} ({currentIndex + 1}/{scanRoots.Count})  Found: {info.FilesFound}  Indexed: {info.FilesIndexed}  Updated: {info.FilesUpdated}  Skipped: {info.FilesSkipped}  Elapsed: {info.Elapsed:hh\\:mm\\:ss}";
                });

                var result = await _services.ScanService
                    .ScanAsync(root.Id, progress, _scanCts.Token, ScanOptions.WholeComputerSafeDefaults)
                    .ConfigureAwait(true);

                aggregate.FilesFound += result.FilesFound;
                aggregate.FilesIndexed += result.FilesIndexed;
                aggregate.FilesUpdated += result.FilesUpdated;
                aggregate.FilesSkipped += result.FilesSkipped;
                aggregate.FilesRemoved += result.FilesRemoved;
                aggregate.Duration += result.Duration;
                IndexingPercent = Math.Min(100d, ((currentIndex + 1d) / scanRoots.Count) * 100d);
            }

            await RefreshScanRootsAsync().ConfigureAwait(true);
            if (scanRoots.Count > 0)
            {
                SelectedScanRoot = ScanRoots.FirstOrDefault(x => x.Id == scanRoots[0].Id) ?? SelectedScanRoot;
            }

            if (SelectedScanRoot is not null)
            {
                await RefreshFiltersAndAlbumsAsync().ConfigureAwait(true);
                await LoadPhotosAsync(reset: true).ConfigureAwait(true);
            }

            StatusMessage =
                $"Whole-computer scan complete across {scanRoots.Count} drive(s). Found {aggregate.FilesFound}, indexed {aggregate.FilesIndexed}, updated {aggregate.FilesUpdated}, removed {aggregate.FilesRemoved}. Safe exclusions reduced app/program image noise.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Whole-computer scan canceled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Whole-computer scan failed: {ex.Message}";
        }
        finally
        {
            IsIndexing = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    private async Task RescanAsync()
    {
        if (SelectedScanRoot is null)
        {
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();

        IsIndexing = true;
        IndexingPercent = 0;
        IndexingStatus = "Scanning files...";
        StatusMessage = "Indexing in progress...";

        try
        {
            var progress = new Progress<ScanProgressInfo>(info =>
            {
                var processed = info.FilesIndexed + info.FilesUpdated + info.FilesSkipped;
                if (info.FilesFound > 0)
                {
                    IndexingPercent = Math.Min(100d, processed * 100d / info.FilesFound);
                }

                IndexingStatus =
                    $"Found: {info.FilesFound}  Indexed: {info.FilesIndexed}  Updated: {info.FilesUpdated}  Skipped: {info.FilesSkipped}  Elapsed: {info.Elapsed:hh\\:mm\\:ss}";
            });

            var result = await _services.ScanService
                .ScanAsync(SelectedScanRoot.Id, progress, _scanCts.Token)
                .ConfigureAwait(true);

            IndexingPercent = 100;
            StatusMessage =
                $"Scan complete. Found {result.FilesFound}, indexed {result.FilesIndexed}, updated {result.FilesUpdated}, removed {result.FilesRemoved}.";

            await RefreshScanRootsAsync().ConfigureAwait(true);
            if (SelectedScanRoot is not null)
            {
                SelectedScanRoot = ScanRoots.FirstOrDefault(x => x.Id == SelectedScanRoot.Id) ?? SelectedScanRoot;
            }

            await RefreshFiltersAndAlbumsAsync().ConfigureAwait(true);
            await LoadPhotosAsync(reset: true).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan canceled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsIndexing = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    private void CancelScan()
    {
        _scanCts?.Cancel();
    }

    private async Task RefreshFiltersAndAlbumsAsync()
    {
        if (SelectedScanRoot is null)
        {
            return;
        }

        var albumData = await _services.PhotoQueryService
            .GetSmartAlbumsAsync(SelectedScanRoot.Id)
            .ConfigureAwait(true);
        SmartAlbums.Clear();
        foreach (var album in albumData)
        {
            SmartAlbums.Add(new SmartAlbumItemViewModel
            {
                Key = album.Key,
                Name = album.Name,
                Count = album.Count
            });
        }

        SelectedAlbum = SmartAlbums.FirstOrDefault(x => x.Key == "all") ?? SmartAlbums.FirstOrDefault();

        var folders = await _services.PhotoQueryService.GetFolderSubpathsAsync(SelectedScanRoot.Id).ConfigureAwait(true);
        FolderFilters.Clear();
        FolderFilters.Add(new FolderFilterItemViewModel { DisplayName = "All Folders", Value = string.Empty });
        foreach (var folder in folders)
        {
            FolderFilters.Add(new FolderFilterItemViewModel
            {
                DisplayName = folder,
                Value = folder
            });
        }

        SelectedFolderFilter = FolderFilters.FirstOrDefault();
        await RefreshDuplicatesAsync().ConfigureAwait(true);
    }

    private async Task RefreshDuplicatesAsync()
    {
        DuplicateGroups.Clear();
        SelectedDuplicateGroup = null;

        if (SelectedScanRoot is null || !EnableDuplicateDetection)
        {
            return;
        }

        var groups = await _services.DuplicateService
            .GetDuplicateGroupsAsync(SelectedScanRoot.Id)
            .ConfigureAwait(true);
        foreach (var group in groups)
        {
            DuplicateGroups.Add(new DuplicateGroupItemViewModel
            {
                Sha256 = group.Sha256,
                Count = group.Count
            });
        }
    }

    private async Task LoadPhotosAsync(bool reset)
    {
        if (SelectedScanRoot is null || _isLoadingPhotos)
        {
            return;
        }

        _isLoadingPhotos = true;
        _queryCts?.Cancel();
        _queryCts?.Dispose();
        _queryCts = new CancellationTokenSource();
        var token = _queryCts.Token;

        if (reset)
        {
            _nextPage = 1;
            _loadedCount = 0;
            Photos.Clear();
            SelectedPhoto = null;
            PreviewImage = null;
        }

        try
        {
            var filter = BuildQueryFilter(_nextPage);
            var result = await _services.PhotoQueryService.QueryPhotosAsync(filter, token).ConfigureAwait(true);

            var newItems = new List<PhotoItemViewModel>();
            foreach (var asset in result.Items)
            {
                var vm = new PhotoItemViewModel(asset);
                Photos.Add(vm);
                newItems.Add(vm);
            }

            TotalResults = result.TotalCount;
            _loadedCount += newItems.Count;
            HasMoreResults = _loadedCount < TotalResults;
            _nextPage++;
            RaisePropertyChanged(nameof(ResultsSummary));

            StatusMessage = TotalResults == 0
                ? "No photos matched the current filters."
                : $"Loaded {_loadedCount} of {TotalResults} photos.";

            _thumbnailCts?.Cancel();
            _thumbnailCts?.Dispose();
            _thumbnailCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _ = LoadThumbnailsAsync(newItems, _thumbnailCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Intentionally ignored because filter and paging changes cancel prior requests.
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load photos: {ex.Message}";
        }
        finally
        {
            _isLoadingPhotos = false;
            RaiseCommandCanExecute();
        }
    }

    private PhotoQueryFilter BuildQueryFilter(int page)
    {
        var albumKey = SelectedDuplicateGroup is not null
            ? $"dup:{SelectedDuplicateGroup.Sha256}"
            : SelectedAlbum?.Key;

        return new PhotoQueryFilter
        {
            ScanRootId = SelectedScanRoot?.Id,
            SearchText = SearchText,
            FromDateUtc = FromDateLocal.HasValue
                ? DateTime.SpecifyKind(FromDateLocal.Value.Date, DateTimeKind.Local).ToUniversalTime()
                : null,
            ToDateUtc = ToDateLocal.HasValue
                ? DateTime.SpecifyKind(ToDateLocal.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime()
                : null,
            DateSource = SelectedDateSource?.Value,
            FolderSubpath = string.IsNullOrWhiteSpace(SelectedFolderFilter?.Value) ? null : SelectedFolderFilter!.Value,
            AlbumKey = albumKey,
            SortBy = SelectedSortOption?.Value ?? PhotoSortOption.DateTakenNewest,
            Page = page,
            PageSize = PageSize
        };
    }

    private async Task LoadThumbnailsAsync(IEnumerable<PhotoItemViewModel> items, CancellationToken cancellationToken)
    {
        var throttler = new SemaphoreSlim(4);
        var tasks = items.Select(async item =>
        {
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = await _services.ThumbnailService
                    .GetThumbnailPathAsync(item.Asset, 220, cancellationToken)
                    .ConfigureAwait(false);
                if (path is null)
                {
                    return;
                }

                var image = ImageLoader.LoadBitmap(path, 220);
                if (image is null)
                {
                    return;
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => item.ThumbnailImage = image);
            }
            catch (OperationCanceledException)
            {
                // Swallow cancellation to keep lazy loading responsive.
            }
            finally
            {
                throttler.Release();
            }
        });

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Swallow cancellation.
        }
    }

    private async Task LoadPreviewAsync(PhotoItemViewModel? selected)
    {
        _previewCts?.Cancel();
        _previewCts?.Dispose();
        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        if (selected is null)
        {
            PreviewImage = null;
            return;
        }

        try
        {
            var image = await Task.Run(() => ImageLoader.LoadBitmap(selected.FullPath, 1200), token).ConfigureAwait(true);
            if (!token.IsCancellationRequested)
            {
                PreviewImage = image;
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation.
        }
    }

    private async Task BuildOrganizerPlanAsync()
    {
        if (SelectedScanRoot is null)
        {
            return;
        }

        try
        {
            var plan = await _services.OrganizerPlanService
                .CreatePlanAsync(new OrganizerPlanRequest { ScanRootId = SelectedScanRoot.Id })
                .ConfigureAwait(true);

            _latestOrganizerPlanItems = plan.Items.ToList();
            OrganizerPreviewItems.Clear();
            foreach (var item in _latestOrganizerPlanItems.Take(100))
            {
                OrganizerPreviewItems.Add(item);
            }

            PlanSummary =
                $"Plan created at {plan.GeneratedUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}. {plan.TotalMoves} of {plan.TotalEvaluated} files can move. Showing first {OrganizerPreviewItems.Count}.";
            StatusMessage = "Organizer plan generated. Review and apply when ready.";
            RaiseCommandCanExecute();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create organizer plan: {ex.Message}";
        }
    }

    private async Task ApplyOrganizerPlanAsync()
    {
        if (SelectedScanRoot is null || _latestOrganizerPlanItems.Count == 0)
        {
            return;
        }

        var confirmation = System.Windows.MessageBox.Show(
            $"Apply organizer plan for {SelectedScanRoot.RootPath}?{Environment.NewLine}" +
            $"Moves to apply: {_latestOrganizerPlanItems.Count}",
            "Apply Organizer Plan",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (confirmation != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsIndexing = true;
            IndexingPercent = 0;
            IndexingStatus = "Applying organizer plan...";
            StatusMessage = "Applying organizer plan...";

            var result = await _services.OrganizerPlanService
                .ApplyPlanAsync(SelectedScanRoot.Id, _latestOrganizerPlanItems)
                .ConfigureAwait(true);

            _latestOrganizerPlanItems.Clear();
            OrganizerPreviewItems.Clear();
            PlanSummary = "No organizer plan generated yet.";

            await RefreshScanRootsAsync().ConfigureAwait(true);
            if (SelectedScanRoot is not null)
            {
                await RefreshFiltersAndAlbumsAsync().ConfigureAwait(true);
                await LoadPhotosAsync(reset: true).ConfigureAwait(true);
            }

            IndexingPercent = 100;
            StatusMessage =
                $"Organizer apply complete. Attempted {result.AttemptedMoves}, moved {result.Moved}, skipped {result.Skipped}, failed {result.Failed}.";
            RaiseCommandCanExecute();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to apply organizer plan: {ex.Message}";
        }
        finally
        {
            IsIndexing = false;
            IndexingStatus = string.Empty;
        }
    }

    private async Task ClearFiltersAsync()
    {
        SearchText = string.Empty;
        FromDateLocal = null;
        ToDateLocal = null;
        SelectedDateSource = DateSourceOptions[0];
        SelectedFolderFilter = FolderFilters.FirstOrDefault();
        SelectedDuplicateGroup = null;
        SelectedAlbum = SmartAlbums.FirstOrDefault(x => x.Key == "all") ?? SmartAlbums.FirstOrDefault();
        await LoadPhotosAsync(reset: true).ConfigureAwait(true);
    }

    private void OpenFileLocation()
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{SelectedPhoto.FullPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to open file location: {ex.Message}";
        }
    }

    private async Task UpdateDuplicateDetectionAsync(bool enabled)
    {
        if (SelectedScanRoot is null)
        {
            return;
        }

        try
        {
            await _services.ScanService
                .UpdateDuplicateDetectionAsync(SelectedScanRoot.Id, enabled)
                .ConfigureAwait(true);
            SelectedScanRoot.EnableDuplicateDetection = enabled;
            StatusMessage = enabled
                ? "Duplicate detection enabled. New scans will compute SHA-256."
                : "Duplicate detection disabled.";
            await RefreshDuplicatesAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to update duplicate detection setting: {ex.Message}";
        }
    }

    private static void ApplyThemePreference(AppThemePreference preference)
    {
        if (System.Windows.Application.Current is App app)
        {
            app.ApplyTheme(preference);
        }
    }

    private void RaiseCommandCanExecute()
    {
        ((AsyncRelayCommand)ScanWholeComputerCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RescanCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CancelScanCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ApplyFiltersCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ClearFiltersCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)LoadMoreCommand).RaiseCanExecuteChanged();
        ((RelayCommand)OpenFileLocationCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)BuildOrganizerPlanCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ApplyOrganizerPlanCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RefreshDuplicatesCommand).RaiseCanExecuteChanged();
    }
}
