using System;

namespace CosmosMaze.Core;

internal sealed class Maze
{
    public readonly int Rows;
    public readonly int Cols;
    private readonly byte[] _cells;

    private Maze(int size)
    {
        Rows = size;
        Cols = size;
        _cells = new byte[size * size];
        for (int i = 0; i < _cells.Length; i++) _cells[i] = 1;
    }

    public bool IsWall(int x, int z)
    {
        if ((uint)x >= (uint)Cols || (uint)z >= (uint)Rows) return true;
        return _cells[z * Cols + x] != 0;
    }

    public void SetCell(int x, int z, byte value)
    {
        _cells[z * Cols + x] = value;
    }

    public static Maze Generate(int size, int seed)
    {
        if ((size & 1) == 0) size += 1;
        Maze maze = new Maze(size);

        int s = seed % 2147483647;
        if (s <= 0) s += 2147483646;

        float NextFloat()
        {
            s = (int)((s * 48271L) % 2147483647L);
            return (s - 1) / 2147483646f;
        }

        void Carve(int cx, int cz)
        {
            maze.SetCell(cx, cz, 0);
            int[] dirs = { 2, 0, -2, 0, 0, 2, 0, -2 };

            for (int i = 6; i > 0; i -= 2)
            {
                int j = (int)(NextFloat() * ((i / 2) + 1)) * 2;
                int t0 = dirs[i];
                int t1 = dirs[i + 1];
                dirs[i] = dirs[j];
                dirs[i + 1] = dirs[j + 1];
                dirs[j] = t0;
                dirs[j + 1] = t1;
            }

            for (int d = 0; d < dirs.Length; d += 2)
            {
                int nx = cx + dirs[d];
                int nz = cz + dirs[d + 1];
                if (nx <= 0 || nz <= 0 || nx >= size - 1 || nz >= size - 1) continue;
                if (maze.IsWall(nx, nz))
                {
                    maze.SetCell(cx + dirs[d] / 2, cz + dirs[d + 1] / 2, 0);
                    Carve(nx, nz);
                }
            }
        }

        Carve(1, 1);
        return maze;
    }
}
