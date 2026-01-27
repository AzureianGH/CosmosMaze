using System;
using System.Runtime.InteropServices;

namespace CosmosMaze.Platform.Mac;

internal static class CoreGraphics
{
    private const string Lib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    public const uint kCGImageAlphaPremultipliedLast = 1;
    public const uint kCGBitmapByteOrder32Big = 4u << 12;

    [DllImport(Lib)]
    public static extern IntPtr CGColorSpaceCreateDeviceRGB();

    [DllImport(Lib)]
    public static extern void CGColorSpaceRelease(IntPtr colorSpace);

    [DllImport(Lib)]
    public static extern IntPtr CGDataProviderCreateWithData(IntPtr info, IntPtr data, IntPtr size, IntPtr releaseCallback);

    [DllImport(Lib)]
    public static extern void CGDataProviderRelease(IntPtr provider);

    [DllImport(Lib)]
    public static extern IntPtr CGImageCreate(
        int width,
        int height,
        int bitsPerComponent,
        int bitsPerPixel,
        int bytesPerRow,
        IntPtr colorSpace,
        uint bitmapInfo,
        IntPtr provider,
        IntPtr decode,
        bool shouldInterpolate,
        int renderingIntent);

    [DllImport(Lib)]
    public static extern void CGImageRelease(IntPtr image);

    [DllImport(Lib)]
    public static extern void CGContextDrawImage(IntPtr context, ObjC.CGRect rect, IntPtr image);

    [DllImport(Lib)]
    public static extern void CGContextSaveGState(IntPtr context);

    [DllImport(Lib)]
    public static extern void CGContextRestoreGState(IntPtr context);

    [DllImport(Lib)]
    public static extern void CGContextTranslateCTM(IntPtr context, double tx, double ty);

    [DllImport(Lib)]
    public static extern void CGContextScaleCTM(IntPtr context, double sx, double sy);
}
