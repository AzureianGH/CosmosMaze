namespace CosmosMaze.Core;

internal sealed class Texture
{
    public readonly int Width;
    public readonly int Height;
    public readonly byte[] Pixels;

    public Texture(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public void Sample(int x, int y, out byte r, out byte g, out byte b, out byte a)
    {
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x >= Width) x = Width - 1;
        if (y >= Height) y = Height - 1;
        int idx = (y * Width + x) * 4;
        r = Pixels[idx];
        g = Pixels[idx + 1];
        b = Pixels[idx + 2];
        a = Pixels[idx + 3];
    }
}
