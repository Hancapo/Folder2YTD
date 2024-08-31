using System.IO;
using DirectXTexNet;
using TeximpNet;
using TeximpNet.Compression;

namespace Folder2YTD;

public abstract class ImageHelper
{
    public static readonly List<string> ValidExtensions = [ ".png", ".jpg", ".bmp", ".tiff", ".tif", ".jpeg", ".dds", ".psd", ".gif", ".ico", ".iff", ".webp"];

    private static bool IsTransparent(Surface image)
    {
        return image.IsTransparent;
    }

    private static int CalculateMipMapLevels(int width, int height)
    {
        if (width <= 4 || height <= 4)
        {
            return 1;
        }

        int levels = 1;

        while (width > 4 && height > 4)
        {
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
            levels++;
        }

        return levels;
    }

    private static Surface ResizeImage(Surface image)
    {
        int imageHeight = image.Height;
        int imageWidth = image.Width;

        double log2Width = Math.Log2(imageWidth);
        double log2Height = Math.Log2(imageHeight);

        if (log2Width % 1 == 0 && log2Height % 1 == 0)
        {
            return image;
        }

        var imageNewSize = PowerOfTwoResize(imageWidth, imageHeight);
        image.Resize(imageNewSize.Item1, imageNewSize.Item2, ImageFilter.Lanczos3);
        return image;
    }

    public static bool ConvertImageToDds(string filePath, CompressionQuality quality)
    {
        Surface image;
        Compressor compressor = new Compressor();

        string fileExtension = Path.GetExtension(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        if (ValidExtensions.Contains(fileExtension))
        {
            try
            {
                image = Surface.LoadFromFile(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        else
        {
            return false;
        }
        Surface resizedImage = ResizeImage(image);
        resizedImage.FlipVertically();
        int height = resizedImage.Height;
        int width = resizedImage.Width;
        int mipMapLevels = CalculateMipMapLevels(width, height);

        compressor.Input.SetData(resizedImage);
        compressor.Input.SetMipmapGeneration(true, mipMapLevels);
        compressor.Input.MipmapFilter = MipmapFilter.Box;
        compressor.Output.OutputFileFormat = OutputFileFormat.DDS;
        compressor.Compression.Quality = quality;
        
        compressor.Compression.Format = IsTransparent(resizedImage) ? CompressionFormat.DXT5 : CompressionFormat.DXT1a;

        compressor.Process($"{Path.GetDirectoryName(filePath)}/{fileName}.dds");


        image.Dispose();
        resizedImage.Dispose();
        compressor.Dispose();

        return true;
    }

    private static (int, int) PowerOfTwoResize(int width, int height)
    {
        width = (int)Math.Pow(2, Math.Round(Math.Log2(width)));
        height = (int)Math.Pow(2, Math.Round(Math.Log2(height)));
        return (width, height);
    }
}