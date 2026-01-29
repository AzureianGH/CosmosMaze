using System;

namespace CosmosMaze.Core.Font;

internal sealed class TextRenderer
{
    private readonly FontCache _cache;

    public TextRenderer(FontCache cache)
    {
        _cache = cache;
    }

    public void DrawText(FrameBuffer fb, string text, int x, int y, int pixelSize, byte r, byte g, byte b)
    {
        if (string.IsNullOrEmpty(text)) return;
        int penX = x;
        float scale = pixelSize / (float)_cache.Font.UnitsPerEm;
        int baseline = y;
        int lineHeight = (int)MathF.Round((_cache.Font.Ascent - _cache.Font.Descent) * scale);

        for (int i = 0; i < text.Length; i++)
        {
            int code = text[i];
            if (code == '\n')
            {
                penX = x;
                baseline += lineHeight;
                continue;
            }

            TtfRasterizer.GlyphBitmap glyph = _cache.GetGlyph(code, pixelSize);
            int gx = penX + glyph.BearingX;
            int gy = baseline - glyph.BearingY;

            int w = glyph.Width;
            int h = glyph.Height;
            byte[] a = glyph.Alpha;
            for (int yy = 0; yy < h; yy++)
            {
                int py = gy + yy;
                if ((uint)py >= (uint)fb.Height) continue;
                int row = yy * w;
                for (int xx = 0; xx < w; xx++)
                {
                    int px = gx + xx;
                    if ((uint)px >= (uint)fb.Width) continue;
                    byte alpha = a[row + xx];
                    if (alpha == 0) continue;

                    float af = alpha / 255f;
                    int idx = (py * fb.Width + px) * 4;
                    byte dr = fb.Pixels[idx];
                    byte dg = fb.Pixels[idx + 1];
                    byte db = fb.Pixels[idx + 2];
                    fb.Pixels[idx] = (byte)(dr + (r - dr) * af);
                    fb.Pixels[idx + 1] = (byte)(dg + (g - dg) * af);
                    fb.Pixels[idx + 2] = (byte)(db + (b - db) * af);
                }
            }

            penX += glyph.Advance;
        }
    }
}
