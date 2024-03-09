using System.IO;
using CodeWalker.GameFiles;
using CodeWalker.Utils;

namespace Folder2YTD;

public abstract class YtdHelper
{
    public static void CreateYtdFilesFromFolders(List<string> images, string forceoutput)
    {
        if (images.Count == 0) return;
        var ytd = new YtdFile
        {
            TextureDict = new TextureDictionary()
        };

        Path.GetDirectoryName(forceoutput);
        ytd.TextureDict.Textures = new ResourcePointerList64<Texture>();
        ytd.TextureDict.TextureNameHashes = new ResourceSimpleList64_uint();
        ytd = TexturesToYtd(TextureListFromDdsFiles(images.ToArray()), ytd);
        var outfpath = forceoutput + ".ytd";
        File.WriteAllBytes(outfpath, ytd.Save());
    }

    private static YtdFile TexturesToYtd(List<Texture> texturesList, YtdFile ytdFile)
    {
        var textureDictionary = ytdFile.TextureDict;
        textureDictionary.BuildFromTextureList(texturesList);
        return ytdFile;
    }

    private static List<Texture> TextureListFromDdsFiles(string[] ddsFiles)
    {
        List<Texture> textureList = [];

        foreach (var ddsFile in ddsFiles)
        {
            var fn = ddsFile;

            if (!File.Exists(fn)) return null!;

            try
            {
                var dds = File.ReadAllBytes(fn);
                var tex = DDSIO.GetTexture(dds);
                tex.Name = Path.GetFileNameWithoutExtension(fn);
                tex.NameHash = JenkHash.GenHash(tex.Name?.ToLowerInvariant());
                JenkIndex.Ensure(tex.Name?.ToLowerInvariant());
                textureList.Add(tex);
            }
            catch
            {
                // ignored
            }
        }

        return textureList;
    }
    
    public static void CreateYtdFilesFromSingleImage(IReadOnlyList<string> images, string outputfolder)
    {
        if (images.Count == 0) return;

        Parallel.For(0, images.Count, i =>
        {
            var ytd = new YtdFile
            {
                TextureDict = new TextureDictionary
                {
                    Textures = new ResourcePointerList64<Texture>(),
                    TextureNameHashes = new ResourceSimpleList64_uint()
                }
            };
            ytd = TexturesToYtd(TextureListFromDdsFiles([images[i]]), ytd);
            var outfpath = outputfolder + "\\" + Path.GetFileNameWithoutExtension(images[i]) + ".ytd";
            File.WriteAllBytes(outfpath, ytd.Save());
        });
    }
}