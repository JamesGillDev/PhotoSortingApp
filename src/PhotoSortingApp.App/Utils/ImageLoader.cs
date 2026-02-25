using System.IO;
using System.Windows.Media.Imaging;

namespace PhotoSortingApp.App.Utils;

public static class ImageLoader
{
    public static BitmapImage? LoadBitmap(string filePath, int decodePixelWidth)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var stream = File.OpenRead(filePath);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        image.DecodePixelWidth = decodePixelWidth;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
