using System.Windows.Media.Imaging;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.App.ViewModels;

public class PhotoItemViewModel : ObservableObject
{
    private BitmapImage? _thumbnailImage;

    public PhotoItemViewModel(PhotoAsset asset)
    {
        Asset = asset;
    }

    public PhotoAsset Asset { get; }

    public int Id => Asset.Id;

    public string FileName => Asset.FileName;

    public string FullPath => Asset.FullPath;

    public string Extension => Asset.Extension;

    public string? Camera => string.IsNullOrWhiteSpace(Asset.CameraMake) && string.IsNullOrWhiteSpace(Asset.CameraModel)
        ? null
        : $"{Asset.CameraMake} {Asset.CameraModel}".Trim();

    public string DateTakenText => Asset.DateTaken?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "(unknown)";

    public string ResolutionText => Asset.Width.HasValue && Asset.Height.HasValue
        ? $"{Asset.Width.Value} x {Asset.Height.Value}"
        : "(unknown)";

    public string FileSizeText => Asset.FileSizeBytes <= 0
        ? "(unknown)"
        : FormatFileSize(Asset.FileSizeBytes);

    public BitmapImage? ThumbnailImage
    {
        get => _thumbnailImage;
        set => SetProperty(ref _thumbnailImage, value);
    }

    private static string FormatFileSize(long bytes)
    {
        const double kb = 1024d;
        const double mb = kb * 1024d;
        const double gb = mb * 1024d;

        if (bytes >= gb)
        {
            return $"{bytes / gb:F2} GB";
        }

        if (bytes >= mb)
        {
            return $"{bytes / mb:F1} MB";
        }

        if (bytes >= kb)
        {
            return $"{bytes / kb:F0} KB";
        }

        return $"{bytes} B";
    }
}
