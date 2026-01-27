using System.IO;
using CosmosMaze.Core;

namespace CosmosMaze.Platform.Mac;

internal sealed class MacAssetLoader : IAssetLoader
{
    private readonly string _basePath;

    public MacAssetLoader(string basePath)
    {
        _basePath = basePath;
    }

    public byte[] LoadBytes(string name)
    {
        string path = Path.Combine(_basePath, name);
        return File.ReadAllBytes(path);
    }
}
