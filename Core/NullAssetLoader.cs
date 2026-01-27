using System;

namespace CosmosMaze.Core;

internal sealed class NullAssetLoader : IAssetLoader
{
    public byte[] LoadBytes(string name)
    {
        return Array.Empty<byte>();
    }
}
