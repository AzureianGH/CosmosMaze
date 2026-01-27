using CosmosMaze.Core;

namespace CosmosMaze.Platform;

internal interface IPlatformWindow
{
    void Run();
    FrameBuffer Frame { get; }
}
