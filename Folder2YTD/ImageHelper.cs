using System.IO;
using DirectXTexNet;

namespace Folder2YTD;

public abstract class ImageHelper
{
    private static readonly List<string> ValidWcExtensions = [ ".png", ".jpg", ".bmp", ".tiff", ".tif", ".jpeg", ".dds"];

    private static bool IsTransparent(ScratchImage image)
    {
        return !image.IsAlphaAllOpaque();
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

    private static ScratchImage ResizeImage(ScratchImage image)
    {
        int imageHeight = image.GetMetadata().Height;
        int imageWidth = image.GetMetadata().Width;

        double log2Width = Math.Log2(imageWidth);
        double log2Height = Math.Log2(imageHeight);

        if (log2Width % 1 == 0 && log2Height % 1 == 0)
        {
            return image;
        }

        var imageNewSize = PowerOfTwoResize(imageWidth, imageHeight);
        ScratchImage resizedImage = image.Resize(0, imageNewSize.Item1, imageNewSize.Item2, TEX_FILTER_FLAGS.LINEAR);
        return resizedImage;
    }

    public static bool ConvertImageToDds(string filePath)
    {
        ScratchImage image;

        string fileExtension = Path.GetExtension(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        if (ValidWcExtensions.Contains(fileExtension))
        {
            try
            {
                image = TexHelper.Instance.LoadFromWICFile(filePath, WIC_FLAGS.NONE);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        else if (fileExtension == ".tga")
        {
            try
            {
                image = TexHelper.Instance.LoadFromTGAFile(filePath);
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
        ScratchImage resizedImage = ResizeImage(image);
        
        int height = resizedImage.GetMetadata().Height;
        int width = resizedImage.GetMetadata().Width;
        int mipMapLevels = CalculateMipMapLevels(width, height);
        
        ScratchImage mipmappedImage = mipMapLevels > 1
            ? resizedImage.GenerateMipMaps(TEX_FILTER_FLAGS.BOX, mipMapLevels)
            : resizedImage;
        ScratchImage compressedImage = IsTransparent(mipmappedImage)
            ? mipmappedImage.Compress(DXGI_FORMAT.BC3_UNORM, TEX_COMPRESS_FLAGS.DEFAULT, 0)
            : mipmappedImage.Compress(DXGI_FORMAT.BC1_UNORM, TEX_COMPRESS_FLAGS.DEFAULT, 0.5f);
        compressedImage.SaveToDDSFile(DDS_FLAGS.NONE, $"{Path.GetDirectoryName(filePath)}/{fileName}.dds");

        image.Dispose();
        resizedImage.Dispose();
        mipmappedImage.Dispose();
        compressedImage.Dispose();

        return true;
    }

    private static (int, int) PowerOfTwoResize(int width, int height)
    {
        width = (int)Math.Pow(2, Math.Round(Math.Log2(width)));
        height = (int)Math.Pow(2, Math.Round(Math.Log2(height)));

        return (width, height);
    }
}