using System;
using CosmosMaze.Core;
using CosmosMaze.Platform.Mac;

namespace CosmosMaze.Platform;

internal static class PlatformFactory
{
    public static IPlatformWindow Create(Game game)
    {
        if (OperatingSystem.IsMacOS())
        {
            return new MacWindow(game);
        }

        throw new NotSupportedException("Only macOS is implemented for now.");
    }
}
