using System;
using System.Runtime.InteropServices;

namespace CosmosMaze.Platform.Mac;

internal static class Native
{
    private const int RtldNow = 2;

    [DllImport("/usr/lib/libSystem.B.dylib")]
    private static extern IntPtr dlopen(string path, int mode);

    public static void LoadFramework(string path)
    {
        dlopen(path, RtldNow);
    }
}
