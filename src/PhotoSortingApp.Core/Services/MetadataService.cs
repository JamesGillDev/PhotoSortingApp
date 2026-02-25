using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Enums;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Services;

public class MetadataService : IMetadataService
{
    public async Task<PhotoMetadataResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ExtractCore(filePath), cancellationToken).ConfigureAwait(false);
    }

    private static PhotoMetadataResult ExtractCore(string filePath)
    {
        var result = new PhotoMetadataResult();

        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var jpeg = directories.OfType<JpegDirectory>().FirstOrDefault();
            var png = directories.OfType<PngDirectory>().FirstOrDefault();

            if (subIfd is not null && subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var exifDate))
            {
                var normalized = exifDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(exifDate, DateTimeKind.Local)
                    : exifDate;
                result.DateTakenUtc = normalized.ToUniversalTime();
                result.DateTakenSource = DateTakenSource.Exif;
            }

            if (ifd0 is not null)
            {
                result.CameraMake = ifd0.GetDescription(ExifDirectoryBase.TagMake)?.Trim();
                result.CameraModel = ifd0.GetDescription(ExifDirectoryBase.TagModel)?.Trim();
            }

            if (subIfd is not null)
            {
                if (subIfd.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out var exifWidth))
                {
                    result.Width = exifWidth;
                }

                if (subIfd.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out var exifHeight))
                {
                    result.Height = exifHeight;
                }
            }

            if (result.Width is null && jpeg is not null && jpeg.TryGetInt32(JpegDirectory.TagImageWidth, out var jpegWidth))
            {
                result.Width = jpegWidth;
            }

            if (result.Height is null && jpeg is not null && jpeg.TryGetInt32(JpegDirectory.TagImageHeight, out var jpegHeight))
            {
                result.Height = jpegHeight;
            }

            if (result.Width is null && png is not null && png.TryGetInt32(PngDirectory.TagImageWidth, out var pngWidth))
            {
                result.Width = pngWidth;
            }

            if (result.Height is null && png is not null && png.TryGetInt32(PngDirectory.TagImageHeight, out var pngHeight))
            {
                result.Height = pngHeight;
            }
        }
        catch
        {
            // Keep scanning resilient: metadata failures must not block indexing.
        }

        if (result.DateTakenUtc is null)
        {
            var createdUtc = File.GetCreationTimeUtc(filePath);
            var modifiedUtc = File.GetLastWriteTimeUtc(filePath);

            if (IsUsableTimestamp(createdUtc))
            {
                result.DateTakenUtc = createdUtc;
                result.DateTakenSource = DateTakenSource.FileCreated;
            }
            else if (IsUsableTimestamp(modifiedUtc))
            {
                result.DateTakenUtc = modifiedUtc;
                result.DateTakenSource = DateTakenSource.FileModified;
            }
            else
            {
                result.DateTakenSource = DateTakenSource.Unknown;
            }
        }

        return result;
    }

    private static bool IsUsableTimestamp(DateTime value)
    {
        return value > DateTime.UnixEpoch && value < DateTime.MaxValue;
    }
}
