using System;

namespace CosmosMaze.Core.Font;

internal sealed class TextRenderer
{
    private readonly FontCache _cache;

    public TextRenderer(FontCache cache)
    {
        _cache = cache;
    }

    public void DrawText(FrameBuffer fb, string text, int x, int y, int pixelSize, byte r, byte g, byte b, bool clearType)
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

                    int idx = (py * fb.Width + px) * 4;
                    byte dr = fb.Pixels[idx];
                    byte dg = fb.Pixels[idx + 1];
                    byte db = fb.Pixels[idx + 2];

                    if (!clearType)
                    {
                        byte alpha = a[row + xx];
                        if (alpha == 0) continue;
                        float af = alpha / 255f;
                        fb.Pixels[idx] = (byte)(dr + (r - dr) * af);
                        fb.Pixels[idx + 1] = (byte)(dg + (g - dg) * af);
                        fb.Pixels[idx + 2] = (byte)(db + (b - db) * af);
                        continue;
                    }

                    float aL = (xx > 0 ? a[row + xx - 1] : (byte)0) / 255f;
                    float aC = a[row + xx] / 255f;
                    float aR = (xx + 1 < w ? a[row + xx + 1] : (byte)0) / 255f;

                    if ((aL + aC + aR) == 0f) continue;

                    float covR = 0.20f * aL + 0.80f * aC;
                    float covG = 0.20f * aL + 0.80f * aC + 0.20f * aR;
                    float covB = 0.20f * aR + 0.80f * aC;

                    float grey = aC;
                    const float colorBlend = 0.80f;
                    covR = grey * (1f - colorBlend) + covR * colorBlend;
                    covG = grey * (1f - colorBlend) + covG * colorBlend;
                    covB = grey * (1f - colorBlend) + covB * colorBlend;

                    if (covR > 1f) covR = 1f;
                    if (covG > 1f) covG = 1f;
                    if (covB > 1f) covB = 1f;

                    fb.Pixels[idx] = (byte)(dr + (r - dr) * covR);
                    fb.Pixels[idx + 1] = (byte)(dg + (g - dg) * covG);
                    fb.Pixels[idx + 2] = (byte)(db + (b - db) * covB);
                }
            }

            penX += glyph.Advance;
        }
    }
}
