using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PhotoSortingApp.App.Services;
using PhotoSortingApp.App.Theming;
using PhotoSortingApp.App.Utils;
using PhotoSortingApp.Domain.Enums;
using PhotoSortingApp.Domain.Models;

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
    private readonly Dictionary<Guid, Func<Task<string>>> _undoHandlers = new();

    private ScanRootItemViewModel? _selectedScanRoot;
    private SmartAlbumItemViewModel? _selectedAlbum;
    private FolderFilterItemViewModel? _selectedFolderFilter;
    private DateSourceOptionViewModel? _selectedDateSource;
    private SortOptionViewModel? _selectedSortOption;
    private LayoutOptionViewModel? _selectedLayoutOption;
    private ThemeOptionViewModel? _selectedThemeOption;
    private DuplicateGroupItemViewModel? _selectedDuplicateGroup;
    private PhotoItemViewModel? _selectedPhoto;
    private RenameSuggestionItemViewModel? _selectedRenameSuggestion;
    private UndoActionItemViewModel? _selectedUndoAction;
    private BitmapImage? _previewImage;
    private DateTime? _fromDateLocal;
    private DateTime? _toDateLocal;
    private string _renameInput = string.Empty;
    private string _tagInputText = string.Empty;
    private string? _selectedTag;
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
        RenameSuggestions = new ObservableCollection<RenameSuggestionItemViewModel>();
        PhotoTags = new ObservableCollection<string>();
        UndoHistory = new ObservableCollection<UndoActionItemViewModel>();

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
        ApplyRenameSuggestionCommand = new RelayCommand(ApplyRenameSuggestion, () => SelectedPhoto is not null && SelectedRenameSuggestion is not null);
        RenamePhotoCommand = new AsyncRelayCommand(RenamePhotoAsync, () => SelectedPhoto is not null && !string.IsNullOrWhiteSpace(RenameInput));
        MovePhotoCommand = new AsyncRelayCommand(MovePhotoAsync, () => SelectedPhoto is not null && !IsIndexing);
        CopyPhotoCommand = new AsyncRelayCommand(CopyPhotoAsync, () => SelectedPhoto is not null && !IsIndexing);
        DuplicatePhotoCommand = new AsyncRelayCommand(DuplicatePhotoAsync, () => SelectedPhoto is not null && !IsIndexing);
        RepairPhotoLocationCommand = new AsyncRelayCommand(RepairPhotoLocationAsync, () => SelectedPhoto is not null && !IsIndexing);
        AddTagCommand = new AsyncRelayCommand(AddTagAsync, () => SelectedPhoto is not null && !string.IsNullOrWhiteSpace(TagInputText));
        UpdateTagCommand = new AsyncRelayCommand(UpdateTagAsync, () => SelectedPhoto is not null && !string.IsNullOrWhiteSpace(SelectedTag) && !string.IsNullOrWhiteSpace(TagInputText));
        RemoveTagCommand = new AsyncRelayCommand(RemoveTagAsync, () => SelectedPhoto is not null && !string.IsNullOrWhiteSpace(SelectedTag));
        UndoLastChangeCommand = new AsyncRelayCommand(UndoLastChangeAsync, () => UndoHistory.Count > 0 && !IsIndexing);
        UndoSelectedChangeCommand = new AsyncRelayCommand(UndoSelectedChangeAsync, () => SelectedUndoAction is not null && !IsIndexing);
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

    public ObservableCollection<RenameSuggestionItemViewModel> RenameSuggestions { get; }

    public ObservableCollection<string> PhotoTags { get; }

    public ObservableCollection<UndoActionItemViewModel> UndoHistory { get; }

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

    public ICommand ApplyRenameSuggestionCommand { get; }

    public ICommand RenamePhotoCommand { get; }

    public ICommand MovePhotoCommand { get; }

    public ICommand CopyPhotoCommand { get; }

    public ICommand DuplicatePhotoCommand { get; }

    public ICommand RepairPhotoLocationCommand { get; }

    public ICommand AddTagCommand { get; }

    public ICommand UpdateTagCommand { get; }

    public ICommand RemoveTagCommand { get; }

    public ICommand UndoLastChangeCommand { get; }

    public ICommand UndoSelectedChangeCommand { get; }

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
            _ = OnSelectedPhotoChangedAsync(value);
            RaiseCommandCanExecute();
        }
    }

    public RenameSuggestionItemViewModel? SelectedRenameSuggestion
    {
        get => _selectedRenameSuggestion;
        set
        {
            if (!SetProperty(ref _selectedRenameSuggestion, value))
            {
                return;
            }

            RaiseCommandCanExecute();
        }
    }

    public UndoActionItemViewModel? SelectedUndoAction
    {
        get => _selectedUndoAction;
        set
        {
            if (!SetProperty(ref _selectedUndoAction, value))
            {
                return;
            }

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

    public string RenameInput
    {
        get => _renameInput;
        set
        {
            if (!SetProperty(ref _renameInput, value))
            {
                return;
            }

            RaiseCommandCanExecute();
        }
    }

    public string TagInputText
    {
        get => _tagInputText;
        set
        {
            if (!SetProperty(ref _tagInputText, value))
            {
                return;
            }

            RaiseCommandCanExecute();
        }
    }

    public string? SelectedTag
    {
        get => _selectedTag;
        set
        {
            if (!SetProperty(ref _selectedTag, value))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                TagInputText = value;
            }

            RaiseCommandCanExecute();
        }
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
            RenameSuggestions.Clear();
            PhotoTags.Clear();
            SelectedTag = null;
            RenameInput = string.Empty;
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
        RenameSuggestions.Clear();
        PhotoTags.Clear();
        SelectedTag = null;
        RenameInput = string.Empty;
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
        var dialog = new OpenFolderDialog
        {
            Title = "Select a root folder that contains photos",
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            return;
        }

        var selected = await _services.ScanService
            .GetOrCreateScanRootAsync(dialog.FolderName, EnableDuplicateDetection)
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
        var reparseLibraryRoots = GetReparseBackedLibraryRoots()
            .Where(path => !fixedDrives.Contains(path, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var scanTargets = new List<(string RootPath, string Label)>
        {
        };
        scanTargets.AddRange(fixedDrives.Select(path => (path, "Drive")));
        scanTargets.AddRange(reparseLibraryRoots.Select(path => (path, "Library")));

        if (scanTargets.Count == 0)
        {
            StatusMessage = "No eligible scan targets were found for whole-computer scan.";
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();

        IsIndexing = true;
        IndexingPercent = 0;
        IndexingStatus = "Preparing whole-computer scan (safe mode)...";
        StatusMessage = reparseLibraryRoots.Count == 0
            ? "Whole-computer scan started. System and program folders are excluded."
            : "Whole-computer scan started. Including reparse-backed user libraries (for example, OneDrive Pictures).";

        try
        {
            var scanRoots = new List<(ScanRoot Root, string Label)>();
            var scanOutcomes = new List<(int ScanRootId, int FilesFound)>();
            foreach (var target in scanTargets)
            {
                _scanCts.Token.ThrowIfCancellationRequested();
                var scanRoot = await _services.ScanService
                    .GetOrCreateScanRootAsync(target.RootPath, EnableDuplicateDetection, _scanCts.Token)
                    .ConfigureAwait(true);
                scanRoots.Add((scanRoot, target.Label));
            }

            var aggregate = new ScanResult();
            for (var index = 0; index < scanRoots.Count; index++)
            {
                _scanCts.Token.ThrowIfCancellationRequested();
                var root = scanRoots[index].Root;
                var label = scanRoots[index].Label;
                var currentIndex = index;
                var progress = new Progress<ScanProgressInfo>(info =>
                {
                    var processed = info.FilesIndexed + info.FilesUpdated + info.FilesSkipped;
                    var drivePercent = info.FilesFound > 0
                        ? Math.Min(100d, processed * 100d / info.FilesFound)
                        : 0d;
                    IndexingPercent = Math.Min(100d, ((currentIndex + (drivePercent / 100d)) / scanRoots.Count) * 100d);
                    IndexingStatus =
                        $"{label} {root.RootPath} ({currentIndex + 1}/{scanRoots.Count})  Found: {info.FilesFound}  Indexed: {info.FilesIndexed}  Updated: {info.FilesUpdated}  Skipped: {info.FilesSkipped}  Elapsed: {info.Elapsed:hh\\:mm\\:ss}";
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
                scanOutcomes.Add((root.Id, result.FilesFound));
                IndexingPercent = Math.Min(100d, ((currentIndex + 1d) / scanRoots.Count) * 100d);
            }

            await RefreshScanRootsAsync().ConfigureAwait(true);
            if (scanRoots.Count > 0)
            {
                var preferredRootId = scanOutcomes
                    .OrderByDescending(x => x.FilesFound)
                    .ThenBy(x => x.ScanRootId)
                    .Select(x => x.ScanRootId)
                    .FirstOrDefault();
                SelectedScanRoot = ScanRoots.FirstOrDefault(x => x.Id == preferredRootId)
                    ?? ScanRoots.FirstOrDefault(x => x.Id == scanRoots[0].Root.Id)
                    ?? SelectedScanRoot;
            }

            if (SelectedScanRoot is not null)
            {
                await RefreshFiltersAndAlbumsAsync().ConfigureAwait(true);
                await LoadPhotosAsync(reset: true).ConfigureAwait(true);
            }

            StatusMessage =
                $"Whole-computer scan complete across {scanRoots.Count} target(s). Found {aggregate.FilesFound}, indexed {aggregate.FilesIndexed}, updated {aggregate.FilesUpdated}, removed {aggregate.FilesRemoved}. Safe exclusions reduced app/program image noise.";
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

    private static IReadOnlyList<string> GetReparseBackedLibraryRoots()
    {
        var candidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Where(Directory.Exists)
            .Where(IsReparsePointDirectory)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsReparsePointDirectory(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch
        {
            return false;
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

    private async Task OnSelectedPhotoChangedAsync(PhotoItemViewModel? selected)
    {
        RenameSuggestions.Clear();
        SelectedRenameSuggestion = null;
        PhotoTags.Clear();
        SelectedTag = null;
        TagInputText = string.Empty;

        if (selected is null)
        {
            RenameInput = string.Empty;
            return;
        }

        RenameInput = Path.GetFileNameWithoutExtension(selected.FileName);

        try
        {
            var tags = await _services.TaggingService.GetTagsAsync(selected.Id).ConfigureAwait(true);
            SetPhotoTags(tags);
            PopulateRenameSuggestions(selected, tags);
        }
        catch (Exception ex)
        {
            PopulateRenameSuggestions(selected, Array.Empty<string>());
            StatusMessage = $"Unable to load tags for selected photo: {ex.Message}";
        }
    }

    private void PopulateRenameSuggestions(PhotoItemViewModel photo, IReadOnlyList<string> tags)
    {
        RenameSuggestions.Clear();

        var extension = NormalizeExtension(photo.Extension);
        var localDate = (photo.Asset.DateTaken ?? photo.Asset.FileLastWriteUtc).ToLocalTime();
        var camera = $"{photo.Asset.CameraMake} {photo.Asset.CameraModel}".Trim();
        var folderName = Path.GetFileName(photo.Asset.FolderPath);

        AddRenameSuggestion($"IMG_{localDate:yyyyMMdd_HHmmss}{extension}");

        if (!string.IsNullOrWhiteSpace(camera))
        {
            AddRenameSuggestion($"{SanitizeFileToken(camera)}_{localDate:yyyyMMdd_HHmmss}{extension}");
        }

        if (tags.Count > 0)
        {
            var tagToken = string.Join("_", tags.Take(3).Select(SanitizeFileToken));
            AddRenameSuggestion($"{tagToken}_{localDate:yyyyMMdd}{extension}");
        }

        if (!string.IsNullOrWhiteSpace(folderName))
        {
            AddRenameSuggestion($"{SanitizeFileToken(folderName)}_{localDate:yyyyMMdd_HHmm}{extension}");
        }

        AddRenameSuggestion($"{SanitizeFileToken(Path.GetFileNameWithoutExtension(photo.FileName))}{extension}");

        SelectedRenameSuggestion = RenameSuggestions.FirstOrDefault();
    }

    private void AddRenameSuggestion(string fileName)
    {
        var normalized = NormalizeSuggestedFileName(fileName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (RenameSuggestions.Any(x => x.FileName.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        RenameSuggestions.Add(new RenameSuggestionItemViewModel
        {
            FileName = normalized,
            DisplayName = normalized
        });
    }

    private void ApplyRenameSuggestion()
    {
        if (SelectedRenameSuggestion is null)
        {
            return;
        }

        RenameInput = Path.GetFileNameWithoutExtension(SelectedRenameSuggestion.FileName);
    }

    private async Task RenamePhotoAsync()
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        var sourcePhoto = SelectedPhoto;
        var oldPath = sourcePhoto.FullPath;
        var oldName = sourcePhoto.FileName;
        var requestedName = BuildRequestedFileName(RenameInput, sourcePhoto.Extension);
        if (string.Equals(oldName, requestedName, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Rename skipped because the file already has that name.";
            return;
        }

        try
        {
            _services.ThumbnailService.Invalidate(sourcePhoto.Asset);
            var updated = await _services.PhotoEditService
                .RenamePhotoAsync(sourcePhoto.Id, requestedName)
                .ConfigureAwait(true);
            if (updated is null)
            {
                StatusMessage = "Unable to rename. The photo record was not found.";
                return;
            }

            PushUndoAction(
                $"Rename {oldName} -> {updated.FileName}",
                async () =>
                {
                    var reverted = await _services.PhotoEditService
                        .RelocatePhotoAsync(updated.Id, oldPath)
                        .ConfigureAwait(true);
                    await RefreshAfterPhotoMutationAsync(updated.Id, refreshFilters: false).ConfigureAwait(true);
                    return reverted is null
                        ? "Undo rename could not find the photo."
                        : $"Undo complete: restored {Path.GetFileName(oldPath)}.";
                });

            await RefreshAfterPhotoMutationAsync(updated.Id, refreshFilters: false).ConfigureAwait(true);
            StatusMessage = $"Renamed '{oldName}' to '{updated.FileName}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Rename failed: {ex.Message}";
        }
    }

    private async Task MovePhotoAsync()
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        var sourcePhoto = SelectedPhoto;
        var oldPath = sourcePhoto.FullPath;
        var dialog = new OpenFolderDialog
        {
            Title = "Move selected photo to folder (inside current scan root)",
            Multiselect = false,
            InitialDirectory = sourcePhoto.Asset.FolderPath
        };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            return;
        }

        try
        {
            _services.ThumbnailService.Invalidate(sourcePhoto.Asset);
            var updated = await _services.PhotoEditService
                .MovePhotoAsync(sourcePhoto.Id, dialog.FolderName)
                .ConfigureAwait(true);
            if (updated is null)
            {
                StatusMessage = "Unable to move. The photo record was not found.";
                return;
            }

            PushUndoAction(
                $"Move {sourcePhoto.FileName} to {updated.FolderPath}",
                async () =>
                {
                    var reverted = await _services.PhotoEditService
                        .RelocatePhotoAsync(updated.Id, oldPath)
                        .ConfigureAwait(true);
                    await RefreshAfterPhotoMutationAsync(updated.Id, refreshFilters: true).ConfigureAwait(true);
                    return reverted is null
                        ? "Undo move could not find the photo."
                        : $"Undo complete: moved back to {Path.GetDirectoryName(oldPath)}.";
                });

            await RefreshAfterPhotoMutationAsync(updated.Id, refreshFilters: true).ConfigureAwait(true);
            StatusMessage = $"Moved '{updated.FileName}' to '{updated.FolderPath}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Move failed: {ex.Message}";
        }
    }

    private async Task CopyPhotoAsync()
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        var sourcePhoto = SelectedPhoto;
        var dialog = new OpenFolderDialog
        {
            Title = "Copy selected photo to folder (inside current scan root)",
            Multiselect = false,
            InitialDirectory = sourcePhoto.Asset.FolderPath
        };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            return;
        }

        try
        {
            var copied = await _services.PhotoEditService
                .CopyPhotoAsync(sourcePhoto.Id, dialog.FolderName)
                .ConfigureAwait(true);
            if (copied is null)
            {
                StatusMessage = "Unable to copy. The photo record was not found.";
                return;
            }

            PushUndoAction(
                $"Copy {sourcePhoto.FileName} to {copied.FolderPath}",
                async () =>
                {
                    var deleted = await _services.PhotoEditService
                        .DeletePhotoAsync(copied.Id, deleteFile: true)
                        .ConfigureAwait(true);
                    await RefreshAfterPhotoMutationAsync(sourcePhoto.Id, refreshFilters: true).ConfigureAwait(true);
                    return deleted
                        ? $"Undo complete: removed copy '{copied.FileName}'."
                        : "Undo copy could not find the copied photo record.";
                });

            await RefreshAfterPhotoMutationAsync(copied.Id, refreshFilters: true).ConfigureAwait(true);
            StatusMessage = $"Copied to '{copied.FullPath}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Copy failed: {ex.Message}";
        }
    }

    private async Task DuplicatePhotoAsync()
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        var sourcePhoto = SelectedPhoto;

        try
        {
            var duplicate = await _services.PhotoEditService
                .DuplicatePhotoAsync(sourcePhoto.Id)
                .ConfigureAwait(true);
            if (duplicate is null)
            {
                StatusMessage = "Unable to duplicate. The photo record was not found.";
                return;
            }

            PushUndoAction(
                $"Duplicate {sourcePhoto.FileName}",
                async () =>
                {
                    var deleted = await _services.PhotoEditService
                        .DeletePhotoAsync(duplicate.Id, deleteFile: true)
                        .ConfigureAwait(true);
                    await RefreshAfterPhotoMutationAsync(sourcePhoto.Id, refreshFilters: true).ConfigureAwait(true);
                    return deleted
                        ? $"Undo complete: removed duplicate '{duplicate.FileName}'."
                        : "Undo duplicate could not find the duplicate photo record.";
                });

            await RefreshAfterPhotoMutationAsync(duplicate.Id, refreshFilters: true).ConfigureAwait(true);
            StatusMessage = $"Created duplicate '{duplicate.FileName}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Duplicate failed: {ex.Message}";
        }
    }

    private async Task RepairPhotoLocationAsync()
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        var sourcePhoto = SelectedPhoto;
        var oldPath = sourcePhoto.FullPath;

        try
        {
            var repaired = await _services.PhotoEditService
                .RepairPhotoLocationAsync(sourcePhoto.Id)
                .ConfigureAwait(true);
            if (repaired is null)
            {
                StatusMessage = "No unique matching file was found to repair the location.";
                return;
            }

            if (string.Equals(oldPath, repaired.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Photo location is already valid.";
                return;
            }

            PushUndoAction(
                $"Repair location for {sourcePhoto.FileName}",
                async () =>
                {
                    var reverted = await _services.PhotoEditService
                        .UpdatePathReferenceAsync(repaired.Id, oldPath)
                        .ConfigureAwait(true);
                    await RefreshAfterPhotoMutationAsync(repaired.Id, refreshFilters: false).ConfigureAwait(true);
                    return reverted is null
                        ? "Undo location repair could not find the photo."
                        : "Undo complete: restored previous path reference.";
                });

            await RefreshAfterPhotoMutationAsync(repaired.Id, refreshFilters: false).ConfigureAwait(true);
            StatusMessage = $"Repaired location to '{repaired.FullPath}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Location repair failed: {ex.Message}";
        }
    }

    private async Task AddTagAsync()
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        var incoming = ParseTagInput(TagInputText);
        if (incoming.Count == 0)
        {
            StatusMessage = "Enter one or more tag values.";
            return;
        }

        var previousTags = PhotoTags.ToList();
        var merged = previousTags
            .Concat(incoming)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (merged.Count == previousTags.Count)
        {
            StatusMessage = "All entered tags are already present.";
            return;
        }

        await ApplyTagChangeAsync(merged, previousTags, "Added tags").ConfigureAwait(true);
    }

    private async Task UpdateTagAsync()
    {
        if (SelectedPhoto is null || string.IsNullOrWhiteSpace(SelectedTag))
        {
            return;
        }

        var replacement = NormalizeTag(TagInputText);
        if (string.IsNullOrWhiteSpace(replacement))
        {
            StatusMessage = "Enter the replacement tag text.";
            return;
        }

        var previousTags = PhotoTags.ToList();
        var updated = previousTags
            .Select(x => x.Equals(SelectedTag, StringComparison.OrdinalIgnoreCase) ? replacement : x)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await ApplyTagChangeAsync(updated, previousTags, $"Updated tag '{SelectedTag}'").ConfigureAwait(true);
    }

    private async Task RemoveTagAsync()
    {
        if (SelectedPhoto is null || string.IsNullOrWhiteSpace(SelectedTag))
        {
            return;
        }

        var previousTags = PhotoTags.ToList();
        var updated = previousTags
            .Where(x => !x.Equals(SelectedTag, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (updated.Count == previousTags.Count)
        {
            return;
        }

        await ApplyTagChangeAsync(updated, previousTags, $"Removed tag '{SelectedTag}'").ConfigureAwait(true);
    }

    private async Task ApplyTagChangeAsync(IReadOnlyList<string> updatedTags, IReadOnlyList<string> previousTags, string description)
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        var photoId = SelectedPhoto.Id;

        try
        {
            var result = await _services.TaggingService
                .ReplaceTagsAsync(photoId, updatedTags)
                .ConfigureAwait(true);
            if (result is null)
            {
                StatusMessage = "Unable to update tags. The photo record was not found.";
                return;
            }

            SetPhotoTags(updatedTags);
            TagInputText = string.Empty;
            SelectedTag = null;

            if (SelectedPhoto?.Id == result.Id)
            {
                PopulateRenameSuggestions(SelectedPhoto, PhotoTags.ToList());
            }

            PushUndoAction(
                $"Tags: {description}",
                async () =>
                {
                    var reverted = await _services.TaggingService
                        .ReplaceTagsAsync(photoId, previousTags)
                        .ConfigureAwait(true);
                    if (SelectedPhoto?.Id == photoId)
                    {
                        SetPhotoTags(previousTags);
                        PopulateRenameSuggestions(SelectedPhoto, PhotoTags.ToList());
                    }

                    return reverted is null
                        ? "Undo tag update could not find the photo."
                        : "Undo complete: restored previous tag set.";
                });

            StatusMessage = $"Tags updated. {PhotoTags.Count} total tag(s) on selected photo.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Tag update failed: {ex.Message}";
        }
    }

    private async Task UndoLastChangeAsync()
    {
        if (UndoHistory.Count == 0)
        {
            return;
        }

        await ExecuteUndoAsync(UndoHistory[0]).ConfigureAwait(true);
    }

    private async Task UndoSelectedChangeAsync()
    {
        if (SelectedUndoAction is null)
        {
            return;
        }

        await ExecuteUndoAsync(SelectedUndoAction).ConfigureAwait(true);
    }

    private async Task ExecuteUndoAsync(UndoActionItemViewModel action)
    {
        if (!_undoHandlers.TryGetValue(action.Id, out var undo))
        {
            UndoHistory.Remove(action);
            SelectedUndoAction = UndoHistory.FirstOrDefault();
            RaiseCommandCanExecute();
            return;
        }

        try
        {
            var message = await undo().ConfigureAwait(true);
            _undoHandlers.Remove(action.Id);
            UndoHistory.Remove(action);
            SelectedUndoAction = UndoHistory.FirstOrDefault();
            StatusMessage = message;
            RaiseCommandCanExecute();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Undo failed: {ex.Message}";
        }
    }

    private void PushUndoAction(string description, Func<Task<string>> undoHandler)
    {
        var item = new UndoActionItemViewModel
        {
            Description = description,
            TimestampLocal = DateTime.Now
        };

        UndoHistory.Insert(0, item);
        _undoHandlers[item.Id] = undoHandler;
        SelectedUndoAction = item;
        RaiseCommandCanExecute();
    }

    private async Task RefreshAfterPhotoMutationAsync(int? preferredPhotoId, bool refreshFilters)
    {
        if (refreshFilters && SelectedScanRoot is not null)
        {
            await RefreshFiltersAndAlbumsAsync().ConfigureAwait(true);
        }
        else
        {
            await RefreshDuplicatesAsync().ConfigureAwait(true);
        }

        await LoadPhotosAsync(reset: true).ConfigureAwait(true);

        if (preferredPhotoId.HasValue)
        {
            SelectedPhoto = Photos.FirstOrDefault(x => x.Id == preferredPhotoId.Value);
        }

        if (SelectedPhoto is null)
        {
            SelectedPhoto = Photos.FirstOrDefault();
        }
    }

    private void SetPhotoTags(IEnumerable<string> tags)
    {
        PhotoTags.Clear();
        foreach (var tag in tags
                     .Select(NormalizeTag)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            PhotoTags.Add(tag);
        }

        RaiseCommandCanExecute();
    }

    private static IReadOnlyList<string> ParseTagInput(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return Array.Empty<string>();
        }

        return rawInput
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeTag)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        var compact = string.Join(' ', tag.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return compact.Replace(',', ' ').Trim();
    }

    private static string BuildRequestedFileName(string requested, string fallbackExtension)
    {
        var requestedName = Path.GetFileName(requested.Trim());
        var rawExt = Path.GetExtension(requestedName);
        var baseName = Path.GetFileNameWithoutExtension(requestedName);
        baseName = SanitizeFileToken(baseName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            throw new InvalidOperationException("The requested file name is invalid.");
        }

        var extension = string.IsNullOrWhiteSpace(rawExt)
            ? NormalizeExtension(fallbackExtension)
            : NormalizeExtension(rawExt);
        return $"{baseName}{extension}";
    }

    private static string NormalizeSuggestedFileName(string fileName)
    {
        var extension = NormalizeExtension(Path.GetExtension(fileName));
        var baseName = SanitizeFileToken(Path.GetFileNameWithoutExtension(fileName));
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return string.Empty;
        }

        return $"{baseName}{extension}";
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".jpg";
        }

        return extension.StartsWith('.')
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";
    }

    private static string SanitizeFileToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(token.Length);
        foreach (var ch in token.Trim())
        {
            sb.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        var compact = string.Join('_', sb.ToString()
            .Split(new[] { ' ', '\t', '-', '_' }, StringSplitOptions.RemoveEmptyEntries));
        if (compact.Length > 80)
        {
            compact = compact[..80];
        }

        return compact.Trim('_');
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
        ((RelayCommand)ApplyRenameSuggestionCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RenamePhotoCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)MovePhotoCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)CopyPhotoCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)DuplicatePhotoCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RepairPhotoLocationCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)AddTagCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)UpdateTagCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RemoveTagCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)UndoLastChangeCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)UndoSelectedChangeCommand).RaiseCanExecuteChanged();
    }
}
