using System;
using System.Collections.Generic;

namespace CosmosMaze.Core.Font;

internal sealed class TtfFont
{
    private readonly byte[] _data;
    private readonly Dictionary<uint, TableRecord> _tables;

    public readonly ushort UnitsPerEm;
    public readonly short Ascent;
    public readonly short Descent;
    public readonly ushort NumGlyphs;
    public readonly ushort NumberOfHMetrics;
    public readonly short IndexToLocFormat;

    private readonly uint[] _loca;
    private readonly ushort[] _advanceWidth;
    private readonly short[] _leftSideBearing;

    private readonly CmapFormat4 _cmap;

    private TtfFont(
        byte[] data,
        Dictionary<uint, TableRecord> tables,
        ushort unitsPerEm,
        short ascent,
        short descent,
        ushort numGlyphs,
        ushort numberOfHMetrics,
        short indexToLocFormat,
        uint[] loca,
        ushort[] advanceWidth,
        short[] leftSideBearing,
        CmapFormat4 cmap)
    {
        _data = data;
        _tables = tables;
        UnitsPerEm = unitsPerEm;
        Ascent = ascent;
        Descent = descent;
        NumGlyphs = numGlyphs;
        NumberOfHMetrics = numberOfHMetrics;
        IndexToLocFormat = indexToLocFormat;
        _loca = loca;
        _advanceWidth = advanceWidth;
        _leftSideBearing = leftSideBearing;
        _cmap = cmap;
    }

    public static TtfFont Load(byte[] data)
    {
        if (data == null || data.Length < 12) throw new ArgumentException("Invalid TTF data.");

        int offset = 0;
        ReadUInt32(data, ref offset);
        ushort numTables = ReadUInt16(data, ref offset);
        offset += 6;

        Dictionary<uint, TableRecord> tables = new Dictionary<uint, TableRecord>(numTables);
        for (int i = 0; i < numTables; i++)
        {
            uint tag = ReadUInt32(data, ref offset);
            uint check = ReadUInt32(data, ref offset);
            uint tblOffset = ReadUInt32(data, ref offset);
            uint length = ReadUInt32(data, ref offset);
            tables[tag] = new TableRecord(tag, check, tblOffset, length);
        }

        ushort unitsPerEm = ReadUInt16AtTable(data, tables, Tag("head"), 18);
        short indexToLocFormat = ReadInt16AtTable(data, tables, Tag("head"), 50);

        short ascent = ReadInt16AtTable(data, tables, Tag("hhea"), 4);
        short descent = ReadInt16AtTable(data, tables, Tag("hhea"), 6);
        ushort numberOfHMetrics = ReadUInt16AtTable(data, tables, Tag("hhea"), 34);

        ushort numGlyphs = ReadUInt16AtTable(data, tables, Tag("maxp"), 4);

        uint[] loca = ReadLoca(data, tables, numGlyphs, indexToLocFormat);
        ReadHmtx(data, tables, numGlyphs, numberOfHMetrics, out ushort[] adv, out short[] lsb);
        CmapFormat4 cmap = ReadCmapFormat4(data, tables);

        return new TtfFont(data, tables, unitsPerEm, ascent, descent, numGlyphs, numberOfHMetrics, indexToLocFormat, loca, adv, lsb, cmap);
    }

    public ushort GetGlyphIndex(int codepoint)
    {
        return _cmap.Map((ushort)codepoint);
    }

    public void GetHorizontalMetrics(ushort glyphIndex, out ushort advanceWidth, out short leftSideBearing)
    {
        if (glyphIndex < _advanceWidth.Length)
        {
            advanceWidth = _advanceWidth[glyphIndex];
            leftSideBearing = _leftSideBearing[glyphIndex];
            return;
        }

        advanceWidth = _advanceWidth[_advanceWidth.Length - 1];
        leftSideBearing = _leftSideBearing[glyphIndex];
    }

    public TtfGlyph LoadGlyph(ushort glyphIndex)
    {
        if (glyphIndex >= _loca.Length - 1) return TtfGlyph.Empty;

        uint glyfOffset = _tables[Tag("glyf")].Offset;
        uint start = _loca[glyphIndex];
        uint end = _loca[glyphIndex + 1];
        if (end <= start) return TtfGlyph.Empty;

        int offset = (int)(glyfOffset + start);
        short numberOfContours = ReadInt16(_data, ref offset);
        short xMin = ReadInt16(_data, ref offset);
        short yMin = ReadInt16(_data, ref offset);
        short xMax = ReadInt16(_data, ref offset);
        short yMax = ReadInt16(_data, ref offset);

        if (numberOfContours <= 0)
        {
            return TtfGlyph.Empty;
        }

        ushort[] endPts = new ushort[numberOfContours];
        for (int i = 0; i < numberOfContours; i++) endPts[i] = ReadUInt16(_data, ref offset);

        ushort instructionLength = ReadUInt16(_data, ref offset);
        offset += instructionLength;

        int pointCount = endPts[numberOfContours - 1] + 1;
        byte[] flags = new byte[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            byte f = _data[offset++];
            flags[i] = f;
            if ((f & 0x08) != 0)
            {
                byte repeat = _data[offset++];
                for (int r = 0; r < repeat; r++)
                {
                    flags[++i] = f;
                }
            }
        }

        short[] xs = new short[pointCount];
        short[] ys = new short[pointCount];

        short x = 0;
        for (int i = 0; i < pointCount; i++)
        {
            byte f = flags[i];
            if ((f & 0x02) != 0)
            {
                byte dx = _data[offset++];
                x += (short)(((f & 0x10) != 0) ? dx : -dx);
            }
            else if ((f & 0x10) == 0)
            {
                x += ReadInt16(_data, ref offset);
            }
            xs[i] = x;
        }

        short y = 0;
        for (int i = 0; i < pointCount; i++)
        {
            byte f = flags[i];
            if ((f & 0x04) != 0)
            {
                byte dy = _data[offset++];
                y += (short)(((f & 0x20) != 0) ? dy : -dy);
            }
            else if ((f & 0x20) == 0)
            {
                y += ReadInt16(_data, ref offset);
            }
            ys[i] = y;
        }

        return new TtfGlyph(numberOfContours, endPts, flags, xs, ys, xMin, yMin, xMax, yMax);
    }

    private static uint[] ReadLoca(byte[] data, Dictionary<uint, TableRecord> tables, ushort numGlyphs, short indexToLocFormat)
    {
        TableRecord loca = tables[Tag("loca")];
        uint[] offsets = new uint[numGlyphs + 1];
        int offset = (int)loca.Offset;
        if (indexToLocFormat == 0)
        {
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = (uint)(ReadUInt16(data, ref offset) * 2);
            }
        }
        else
        {
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = ReadUInt32(data, ref offset);
            }
        }
        return offsets;
    }

    private static void ReadHmtx(byte[] data, Dictionary<uint, TableRecord> tables, ushort numGlyphs, ushort numHMetrics, out ushort[] adv, out short[] lsb)
    {
        adv = new ushort[numGlyphs];
        lsb = new short[numGlyphs];
        TableRecord hmtx = tables[Tag("hmtx")];
        int offset = (int)hmtx.Offset;

        ushort lastAdv = 0;
        for (int i = 0; i < numGlyphs; i++)
        {
            if (i < numHMetrics)
            {
                lastAdv = ReadUInt16(data, ref offset);
                adv[i] = lastAdv;
                lsb[i] = ReadInt16(data, ref offset);
            }
            else
            {
                adv[i] = lastAdv;
                lsb[i] = ReadInt16(data, ref offset);
            }
        }
    }

    private static CmapFormat4 ReadCmapFormat4(byte[] data, Dictionary<uint, TableRecord> tables)
    {
        TableRecord cmap = tables[Tag("cmap")];
        int offset = (int)cmap.Offset;
        offset += 2;
        ushort numTables = ReadUInt16(data, ref offset);

        int subOffset = 0;
        for (int i = 0; i < numTables; i++)
        {
            ushort platform = ReadUInt16(data, ref offset);
            ushort encoding = ReadUInt16(data, ref offset);
            uint sub = ReadUInt32(data, ref offset);
            if (platform == 3 && (encoding == 1 || encoding == 0))
            {
                subOffset = (int)(cmap.Offset + sub);
            }
        }

        if (subOffset == 0) throw new ArgumentException("Unsupported cmap.");

        int so = subOffset;
        ushort format = ReadUInt16(data, ref so);
        if (format != 4) throw new ArgumentException("Unsupported cmap format.");
        ushort length = ReadUInt16(data, ref so);
        so += 2;
        ushort segCountX2 = ReadUInt16(data, ref so);
        int segCount = segCountX2 / 2;
        so += 6;

        ushort[] endCount = new ushort[segCount];
        for (int i = 0; i < segCount; i++) endCount[i] = ReadUInt16(data, ref so);
        so += 2;
        ushort[] startCount = new ushort[segCount];
        for (int i = 0; i < segCount; i++) startCount[i] = ReadUInt16(data, ref so);
        short[] idDelta = new short[segCount];
        for (int i = 0; i < segCount; i++) idDelta[i] = ReadInt16(data, ref so);
        ushort[] idRangeOffset = new ushort[segCount];
        int idRangeOffsetStart = so;
        for (int i = 0; i < segCount; i++) idRangeOffset[i] = ReadUInt16(data, ref so);

        return new CmapFormat4(data, subOffset, segCount, startCount, endCount, idDelta, idRangeOffset, idRangeOffsetStart);
    }

    private static ushort ReadUInt16AtTable(byte[] data, Dictionary<uint, TableRecord> tables, uint tag, int offset)
    {
        int o = (int)tables[tag].Offset + offset;
        return (ushort)(data[o] << 8 | data[o + 1]);
    }

    private static short ReadInt16AtTable(byte[] data, Dictionary<uint, TableRecord> tables, uint tag, int offset)
    {
        int o = (int)tables[tag].Offset + offset;
        return (short)(data[o] << 8 | data[o + 1]);
    }

    private static ushort ReadUInt16(byte[] data, ref int offset)
    {
        ushort v = (ushort)(data[offset] << 8 | data[offset + 1]);
        offset += 2;
        return v;
    }

    private static short ReadInt16(byte[] data, ref int offset)
    {
        short v = (short)(data[offset] << 8 | data[offset + 1]);
        offset += 2;
        return v;
    }

    private static uint ReadUInt32(byte[] data, ref int offset)
    {
        uint v = (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
        offset += 4;
        return v;
    }

    private static uint Tag(string s)
    {
        return ((uint)s[0] << 24) | ((uint)s[1] << 16) | ((uint)s[2] << 8) | s[3];
    }

    private readonly struct TableRecord
    {
        public readonly uint Tag;
        public readonly uint Checksum;
        public readonly uint Offset;
        public readonly uint Length;

        public TableRecord(uint tag, uint checksum, uint offset, uint length)
        {
            Tag = tag;
            Checksum = checksum;
            Offset = offset;
            Length = length;
        }
    }

    private sealed class CmapFormat4
    {
        private readonly byte[] _data;
        private readonly int _subTableOffset;
        private readonly int _segCount;
        private readonly ushort[] _start;
        private readonly ushort[] _end;
        private readonly short[] _delta;
        private readonly ushort[] _rangeOffset;
        private readonly int _rangeOffsetStart;

        public CmapFormat4(byte[] data, int subTableOffset, int segCount, ushort[] start, ushort[] end, short[] delta, ushort[] rangeOffset, int rangeOffsetStart)
        {
            _data = data;
            _subTableOffset = subTableOffset;
            _segCount = segCount;
            _start = start;
            _end = end;
            _delta = delta;
            _rangeOffset = rangeOffset;
            _rangeOffsetStart = rangeOffsetStart;
        }

        public ushort Map(ushort code)
        {
            for (int i = 0; i < _segCount; i++)
            {
                if (code > _end[i]) continue;
                if (code < _start[i]) return 0;

                if (_rangeOffset[i] == 0)
                {
                    return (ushort)(code + _delta[i]);
                }

                int ro = _rangeOffset[i];
                int pos = _rangeOffsetStart + i * 2 + ro + (code - _start[i]) * 2;
                ushort glyph = (ushort)(_data[pos] << 8 | _data[pos + 1]);
                if (glyph == 0) return 0;
                return (ushort)(glyph + _delta[i]);
            }
            return 0;
        }
    }
}
