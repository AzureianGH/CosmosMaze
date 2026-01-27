using System;
using System.IO;
using CosmosMaze.Core;
using CosmosMaze.Platform;
using CosmosMaze.Platform.Mac;

namespace CosmosMaze;

internal static class Program
{
    private static void Main()
    {
        IAssetLoader loader;
        if (OperatingSystem.IsMacOS())
        {
            string basePath = Path.Combine(AppContext.BaseDirectory, "assets");
            loader = new MacAssetLoader(basePath);
        }
        else
        {
            loader = new NullAssetLoader();
        }

        Game game = new Game(loader);
        IPlatformWindow window = PlatformFactory.Create(game);
        window.Run();
    }
}
