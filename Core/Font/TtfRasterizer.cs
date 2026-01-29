using System;
using System.Collections.Generic;

namespace CosmosMaze.Core.Font;

internal sealed class TtfRasterizer
{
    public GlyphBitmap Rasterize(TtfFont font, ushort glyphIndex, int pixelSize)
    {
        if (pixelSize <= 0) pixelSize = 1;
        TtfGlyph glyph = font.LoadGlyph(glyphIndex);
        if (glyph.NumberOfContours <= 0)
        {
            font.GetHorizontalMetrics(glyphIndex, out ushort adv, out short lsb);
            int advanceEmpty = (int)MathF.Round(adv * (pixelSize / (float)font.UnitsPerEm));
            return new GlyphBitmap(0, 0, 0, 0, advanceEmpty, new byte[0]);
        }

        float scale = pixelSize / (float)font.UnitsPerEm;
        int width = (int)MathF.Ceiling((glyph.XMax - glyph.XMin) * scale) + 2;
        int height = (int)MathF.Ceiling((glyph.YMax - glyph.YMin) * scale) + 2;
        if (width < 1) width = 1;
        if (height < 1) height = 1;

        font.GetHorizontalMetrics(glyphIndex, out ushort advanceWidth, out short leftSideBearing);
        int bearingX = (int)MathF.Round(leftSideBearing * scale);
        int bearingY = (int)MathF.Round(glyph.YMax * scale);

        List<List<PointF>> contours = BuildContours(glyph);
        List<List<PointF>> polys = new List<List<PointF>>(contours.Count);
        for (int i = 0; i < contours.Count; i++)
        {
            List<PointF> normalized = NormalizeContour(contours[i]);
            polys.Add(FlattenContour(normalized, scale, -glyph.XMin * scale + 1, -glyph.YMin * scale + 1));
        }

        byte[] alpha = new byte[width * height];
        List<Intersection> intersections = new List<Intersection>(128);
        for (int y = 0; y < height; y++)
        {
            float scanY = y + 0.5f;
            intersections.Clear();
            for (int c = 0; c < polys.Count; c++)
            {
                List<PointF> p = polys[c];
                int count = p.Count;
                for (int i = 0; i < count; i++)
                {
                    PointF a = p[i];
                    PointF b = p[(i + 1) % count];
                    bool crosses = (a.Y <= scanY && b.Y > scanY) || (b.Y <= scanY && a.Y > scanY);
                    if (!crosses) continue;
                    float t = (scanY - a.Y) / (b.Y - a.Y);
                    float x = a.X + t * (b.X - a.X);
                    int winding = b.Y > a.Y ? 1 : -1;
                    intersections.Add(new Intersection(x, winding));
                }
            }

            intersections.Sort();
            int windingCount = 0;
            float spanStart = 0f;
            bool inSpan = false;
            for (int i = 0; i < intersections.Count; i++)
            {
                Intersection inter = intersections[i];
                int prev = windingCount;
                windingCount += inter.Winding;
                if (prev == 0 && windingCount != 0)
                {
                    spanStart = inter.X;
                    inSpan = true;
                    continue;
                }

                if (prev != 0 && windingCount == 0 && inSpan)
                {
                    float spanEnd = inter.X;
                    if (spanEnd < spanStart)
                    {
                        float tmp = spanStart;
                        spanStart = spanEnd;
                        spanEnd = tmp;
                    }

                    int x0 = (int)MathF.Ceiling(spanStart);
                    int x1 = (int)MathF.Floor(spanEnd);
                    if (x1 < 0 || x0 >= width)
                    {
                        inSpan = false;
                        continue;
                    }
                    if (x0 < 0) x0 = 0;
                    if (x1 >= width) x1 = width - 1;
                    int yy = (height - 1 - y);
                    int idx = yy * width + x0;
                    for (int x = x0; x <= x1; x++)
                    {
                        alpha[idx++] = 255;
                    }
                    inSpan = false;
                }
            }
        }

        int advance = (int)MathF.Round(advanceWidth * scale);
        return new GlyphBitmap(width, height, bearingX, bearingY, advance, alpha);
    }

    private static List<List<PointF>> BuildContours(TtfGlyph glyph)
    {
        List<List<PointF>> contours = new List<List<PointF>>();
        int start = 0;
        for (int c = 0; c < glyph.NumberOfContours; c++)
        {
            int end = glyph.EndPts[c];
            List<PointF> contour = new List<PointF>();
            for (int i = start; i <= end; i++)
            {
                bool on = (glyph.Flags[i] & 0x01) != 0;
                contour.Add(new PointF(glyph.Xs[i], glyph.Ys[i], on));
            }
            contours.Add(contour);
            start = end + 1;
        }
        return contours;
    }

    private static List<PointF> NormalizeContour(List<PointF> points)
    {
        if (points.Count == 0) return points;

        List<PointF> pts = new List<PointF>(points.Count + 2);
        for (int i = 0; i < points.Count; i++) pts.Add(points[i]);

        int count = pts.Count;
        if (!pts[0].On)
        {
            PointF last = pts[count - 1];
            if (!last.On)
            {
                PointF mid = new PointF((last.X + pts[0].X) * 0.5f, (last.Y + pts[0].Y) * 0.5f, true);
                pts.Insert(0, mid);
            }
            else
            {
                pts.Insert(0, last);
            }
        }

        for (int i = 0; i < pts.Count; i++)
        {
            PointF a = pts[i];
            PointF b = pts[(i + 1) % pts.Count];
            if (!a.On && !b.On)
            {
                PointF mid = new PointF((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f, true);
                pts.Insert(i + 1, mid);
            }
        }

        return pts;
    }

    private static List<PointF> FlattenContour(List<PointF> points, float scale, float offsetX, float offsetY)
    {
        List<PointF> outPts = new List<PointF>();
        if (points.Count == 0) return outPts;

        int count = points.Count;
        int i = 0;
        while (i < count)
        {
            PointF curr = points[i];
            PointF next = points[(i + 1) % count];
            if (curr.On && next.On)
            {
                outPts.Add(Scale(curr, scale, offsetX, offsetY));
                i++;
                continue;
            }

            if (curr.On && !next.On)
            {
                PointF next2 = points[(i + 2) % count];
                int steps = 8;
                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    float it = 1f - t;
                    float x = it * it * curr.X + 2 * it * t * next.X + t * t * next2.X;
                    float y = it * it * curr.Y + 2 * it * t * next.Y + t * t * next2.Y;
                    outPts.Add(Scale(new PointF(x, y, true), scale, offsetX, offsetY));
                }
                i += 2;
                continue;
            }

            i++;
        }

        return outPts;
    }

    private static PointF Scale(PointF p, float scale, float offsetX, float offsetY)
    {
        return new PointF(p.X * scale + offsetX, p.Y * scale + offsetY, true);
    }

    internal readonly struct GlyphBitmap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int BearingX;
        public readonly int BearingY;
        public readonly int Advance;
        public readonly byte[] Alpha;

        public GlyphBitmap(int width, int height, int bearingX, int bearingY, int advance, byte[] alpha)
        {
            Width = width;
            Height = height;
            BearingX = bearingX;
            BearingY = bearingY;
            Advance = advance;
            Alpha = alpha;
        }
    }

    internal readonly struct PointF
    {
        public readonly float X;
        public readonly float Y;
        public readonly bool On;

        public PointF(float x, float y, bool on)
        {
            X = x;
            Y = y;
            On = on;
        }
    }

    private readonly struct Intersection : IComparable<Intersection>
    {
        public readonly float X;
        public readonly int Winding;

        public Intersection(float x, int winding)
        {
            X = x;
            Winding = winding;
        }

        public int CompareTo(Intersection other)
        {
            return X.CompareTo(other.X);
        }
    }
}
