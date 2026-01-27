using System;

namespace CosmosMaze.Core;

internal static class Bmp
{
    public static Texture Decode(byte[] data)
    {
        if (data == null || data.Length < 54) throw new ArgumentException("Invalid BMP data.");
        if (data[0] != 'B' || data[1] != 'M') throw new ArgumentException("Not a BMP file.");

        int dataOffset = ReadInt32(data, 10);
        int headerSize = ReadInt32(data, 14);
        if (headerSize < 40) throw new ArgumentException("Unsupported BMP header.");

        int width = ReadInt32(data, 18);
        int height = ReadInt32(data, 22);
        bool topDown = false;
        if (height < 0)
        {
            topDown = true;
            height = -height;
        }

        int planes = ReadInt16(data, 26);
        int bpp = ReadInt16(data, 28);
        int compression = ReadInt32(data, 30);
        int clrUsed = ReadInt32(data, 46);

        if (planes != 1) throw new ArgumentException("Unsupported BMP planes.");
        if (compression != 0 && compression != 3) throw new ArgumentException("Unsupported BMP compression.");
        if (bpp != 32 && bpp != 24 && bpp != 8) throw new ArgumentException("Unsupported BMP bpp.");

        int rowSize = ((bpp * width + 31) / 32) * 4;
        byte[] pixels = new byte[width * height * 4];

        int paletteOffset = 14 + headerSize;
        int paletteCount = 0;
        if (bpp == 8)
        {
            paletteCount = clrUsed > 0 ? clrUsed : 256;
            if (paletteOffset + paletteCount * 4 > dataOffset) throw new ArgumentException("Invalid BMP palette.");
        }

        uint maskR = 0x00FF0000;
        uint maskG = 0x0000FF00;
        uint maskB = 0x000000FF;
        uint maskA = 0xFF000000;
        if (compression == 3)
        {
            if (bpp != 32) throw new ArgumentException("Unsupported BMP bitfields for bpp.");
            int maskOffset = 14 + 40;
            if (maskOffset + 12 <= data.Length)
            {
                maskR = ReadUInt32(data, maskOffset + 0);
                maskG = ReadUInt32(data, maskOffset + 4);
                maskB = ReadUInt32(data, maskOffset + 8);
                if (maskOffset + 16 <= data.Length)
                {
                    maskA = ReadUInt32(data, maskOffset + 12);
                }
                else
                {
                    maskA = 0xFF000000;
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            int srcY = topDown ? y : (height - 1 - y);
            int srcRow = dataOffset + srcY * rowSize;
            int dstRow = y * width * 4;

            if (bpp == 32)
            {
                for (int x = 0; x < width; x++)
                {
                    int src = srcRow + x * 4;
                    int dst = dstRow + x * 4;
                    if (compression == 3)
                    {
                        uint v = ReadUInt32(data, src);
                        byte r = ExtractComponent(v, maskR);
                        byte g = ExtractComponent(v, maskG);
                        byte b = ExtractComponent(v, maskB);
                        byte a = maskA != 0 ? ExtractComponent(v, maskA) : (byte)255;
                        pixels[dst] = r;
                        pixels[dst + 1] = g;
                        pixels[dst + 2] = b;
                        pixels[dst + 3] = a;
                    }
                    else
                    {
                        byte b = data[src];
                        byte g = data[src + 1];
                        byte r = data[src + 2];
                        byte a = data[src + 3];
                        pixels[dst] = r;
                        pixels[dst + 1] = g;
                        pixels[dst + 2] = b;
                        pixels[dst + 3] = a;
                    }
                }
            }
            else if (bpp == 24)
            {
                for (int x = 0; x < width; x++)
                {
                    int src = srcRow + x * 3;
                    int dst = dstRow + x * 4;
                    byte b = data[src];
                    byte g = data[src + 1];
                    byte r = data[src + 2];
                    pixels[dst] = r;
                    pixels[dst + 1] = g;
                    pixels[dst + 2] = b;
                    pixels[dst + 3] = 255;
                }
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    byte idx = data[srcRow + x];
                    int pal = paletteOffset + idx * 4;
                    int dst = dstRow + x * 4;
                    byte b = data[pal];
                    byte g = data[pal + 1];
                    byte r = data[pal + 2];
                    pixels[dst] = r;
                    pixels[dst + 1] = g;
                    pixels[dst + 2] = b;
                    pixels[dst + 3] = 255;
                }
            }
        }

        return new Texture(width, height, pixels);
    }

    private static int ReadInt16(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8);
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }

    private static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private static byte ExtractComponent(uint value, uint mask)
    {
        if (mask == 0) return 0;
        int shift = 0;
        uint m = mask;
        while ((m & 1) == 0)
        {
            m >>= 1;
            shift++;
        }
        uint raw = (value & mask) >> shift;
        int bits = 0;
        while ((m & 1) == 1)
        {
            m >>= 1;
            bits++;
        }
        if (bits <= 0) return 0;
        if (bits >= 8) return (byte)raw;
        uint max = (uint)((1 << bits) - 1);
        return (byte)((raw * 255) / max);
    }
}
