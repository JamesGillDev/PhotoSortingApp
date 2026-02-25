using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PhotoSortingApp.Core.Infrastructure;
using PhotoSortingApp.Core.Interfaces;
using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Services;

public class ThumbnailService : IThumbnailService
{
    private readonly string _cacheDirectory;

    public ThumbnailService(string? baseDirectory = null)
    {
        _cacheDirectory = StoragePaths.GetThumbnailCacheDirectory(baseDirectory);
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<string?> GetThumbnailPathAsync(PhotoAsset asset, int maxPixelSize = 256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(asset.FullPath) || !File.Exists(asset.FullPath))
        {
            return null;
        }

        var cacheName = BuildCacheFileName(asset.FullPath, asset.FileLastWriteUtc, maxPixelSize);
        var cachePath = Path.Combine(_cacheDirectory, cacheName);

        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        try
        {
            await Task.Run(() => CreateThumbnail(asset.FullPath, cachePath, maxPixelSize), cancellationToken).ConfigureAwait(false);
            return cachePath;
        }
        catch
        {
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }

            return null;
        }
    }

    public void Invalidate(PhotoAsset asset)
    {
        if (string.IsNullOrWhiteSpace(asset.FullPath))
        {
            return;
        }

        var prefix = Hashing.ComputeSha1ForText(asset.FullPath);
        var candidates = Directory.EnumerateFiles(_cacheDirectory, $"{prefix}_*.jpg");
        foreach (var file in candidates)
        {
            File.Delete(file);
        }
    }

    private static string BuildCacheFileName(string fullPath, DateTime fileLastWriteUtc, int maxPixelSize)
    {
        var key = Hashing.ComputeSha1ForText(fullPath);
        return $"{key}_{fileLastWriteUtc.Ticks}_{maxPixelSize}.jpg";
    }

    private static void CreateThumbnail(string sourcePath, string targetPath, int maxPixelSize)
    {
        using var image = Image.FromFile(sourcePath);
        var scale = Math.Min((double)maxPixelSize / image.Width, (double)maxPixelSize / image.Height);
        scale = Math.Min(scale, 1.0d);

        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(image.Height * scale));

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.DrawImage(image, 0, 0, width, height);

        var encoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        if (encoder is null)
        {
            bitmap.Save(targetPath, ImageFormat.Jpeg);
            return;
        }

        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
        bitmap.Save(targetPath, encoder, parameters);
    }
}
