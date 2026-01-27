using CosmosMaze.Core;
using CosmosMaze.Platform;

namespace CosmosMaze;

internal static class Program
{
    private static void Main()
    {
        Game game = new Game();
        IPlatformWindow window = PlatformFactory.Create(game);
        window.Run();
    }
}
