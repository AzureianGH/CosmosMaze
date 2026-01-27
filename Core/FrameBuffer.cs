using System;

namespace CosmosMaze.Core;

internal sealed class FrameBuffer
{
    public readonly int Width;
    public readonly int Height;
    public readonly byte[] Pixels;

    public FrameBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[width * height * 4];
    }

    public void Clear(byte r, byte g, byte b)
    {
        int len = Pixels.Length;
        for (int i = 0; i < len; i += 4)
        {
            Pixels[i] = r;
            Pixels[i + 1] = g;
            Pixels[i + 2] = b;
            Pixels[i + 3] = 255;
        }
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        int idx = (y * Width + x) * 4;
        Pixels[idx] = r;
        Pixels[idx + 1] = g;
        Pixels[idx + 2] = b;
        Pixels[idx + 3] = 255;
    }

    public void DrawRect(int x, int y, int w, int h, byte r, byte g, byte b)
    {
        if (w <= 0 || h <= 0) return;
        int x0 = x;
        int y0 = y;
        int x1 = x + w;
        int y1 = y + h;
        if (x1 <= 0 || y1 <= 0 || x0 >= Width || y0 >= Height) return;
        if (x0 < 0) x0 = 0;
        if (y0 < 0) y0 = 0;
        if (x1 > Width) x1 = Width;
        if (y1 > Height) y1 = Height;

        for (int yy = y0; yy < y1; yy++)
        {
            int row = (yy * Width + x0) * 4;
            int end = (yy * Width + x1) * 4;
            for (int i = row; i < end; i += 4)
            {
                Pixels[i] = r;
                Pixels[i + 1] = g;
                Pixels[i + 2] = b;
                Pixels[i + 3] = 255;
            }
        }
    }

    public void DrawColumn(int x, int top, int bottom, int thickness, byte r, byte g, byte b)
    {
        if (thickness <= 0) return;
        int left = x - thickness / 2;
        int right = left + thickness;
        if (right <= 0 || left >= Width) return;
        if (top < 0) top = 0;
        if (bottom > Height) bottom = Height;
        if (bottom <= top) return;
        if (left < 0) left = 0;
        if (right > Width) right = Width;

        for (int yy = top; yy < bottom; yy++)
        {
            int row = (yy * Width + left) * 4;
            int end = (yy * Width + right) * 4;
            for (int i = row; i < end; i += 4)
            {
                Pixels[i] = r;
                Pixels[i + 1] = g;
                Pixels[i + 2] = b;
                Pixels[i + 3] = 255;
            }
        }
    }
}
