namespace CosmosMaze.Core.Font;

internal sealed class TtfGlyph
{
    public static readonly TtfGlyph Empty = new TtfGlyph(0, new ushort[0], new byte[0], new short[0], new short[0], 0, 0, 0, 0);

    public readonly short NumberOfContours;
    public readonly ushort[] EndPts;
    public readonly byte[] Flags;
    public readonly short[] Xs;
    public readonly short[] Ys;
    public readonly short XMin;
    public readonly short YMin;
    public readonly short XMax;
    public readonly short YMax;

    public TtfGlyph(short numberOfContours, ushort[] endPts, byte[] flags, short[] xs, short[] ys, short xMin, short yMin, short xMax, short yMax)
    {
        NumberOfContours = numberOfContours;
        EndPts = endPts;
        Flags = flags;
        Xs = xs;
        Ys = ys;
        XMin = xMin;
        YMin = yMin;
        XMax = xMax;
        YMax = yMax;
    }
}
