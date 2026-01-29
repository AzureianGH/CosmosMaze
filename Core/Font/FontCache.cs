using System.Collections.Generic;

namespace CosmosMaze.Core.Font;

internal sealed class FontCache
{
    private readonly TtfFont _font;
    private readonly TtfRasterizer _rasterizer;
    private readonly Dictionary<ulong, TtfRasterizer.GlyphBitmap> _cache;

    public FontCache(TtfFont font)
    {
        _font = font;
        _rasterizer = new TtfRasterizer();
        _cache = new Dictionary<ulong, TtfRasterizer.GlyphBitmap>();
    }

    public TtfRasterizer.GlyphBitmap GetGlyph(int codepoint, int pixelSize)
    {
        ushort glyph = _font.GetGlyphIndex(codepoint);
        ulong key = ((ulong)glyph << 32) | (uint)pixelSize;
        if (_cache.TryGetValue(key, out TtfRasterizer.GlyphBitmap bmp)) return bmp;
        bmp = _rasterizer.Rasterize(_font, glyph, pixelSize);
        _cache[key] = bmp;
        return bmp;
    }

    public TtfFont Font => _font;
}
