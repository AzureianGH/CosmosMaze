using System;

namespace CosmosMaze.Core;

internal sealed class Game
{
    public const int ScreenW = 600;
    public const int ScreenH = 600;

    private const float Near = 10f;
    private const float MoveSpeed = 140f;
    private const float LookSpeed = 1.5f;
    private const float EyeHeightRatio = 0.72f;

    private readonly FrameBuffer _frame;
    private readonly float[] _wallDepth;
    private readonly int[] _levelSizes;

    private float _camX;
    private float _camY;
    private float _camZ;
    private float _yaw;
    private float _pitch;
    private bool _flyMode;
    private bool _minimapOn;

    private int _levelIndex;
    private int _levelSeed;

    private Maze _maze = null!;
    private int _rows;
    private int _cols;
    private float _cell;
    private float _wallH;
    private float _startX;
    private float _startZ;

    private float _goalX;
    private float _goalZ;
    private bool _spawned;
    private bool _goalPlaced;
    private int _goalLevelIndex;
    private byte _wallColorR;
    private byte _wallColorG;
    private byte _wallColorB;

    public Game()
    {
        _frame = new FrameBuffer(ScreenW, ScreenH);
        _wallDepth = new float[ScreenW];
        _levelSizes = new[] { 5, 7, 9, 11, 13, 15, 17, 19, 21, 25 };
        _levelIndex = 0;
        _levelSeed = (int)(DateTime.UtcNow.Ticks % 1000000000L) + 1;
        BuildLevel();
    }

    public FrameBuffer Frame => _frame;

    public void Tick(float dt)
    {
        Update(dt);
        Render();
    }

    private void BuildLevel()
    {
        int size = _levelSizes[_levelIndex % _levelSizes.Length];
        _maze = Maze.Generate(size, _levelSeed);
        _rows = _maze.Rows;
        _cols = _maze.Cols;
        _cell = 80f;
        _wallH = 80f;
        _startX = -_cols * _cell * 0.5f;
        _startZ = -_rows * _cell * 0.5f;
        _spawned = false;
        _goalPlaced = false;
        PickWallColor();
    }

    private void PickWallColor()
    {
        int s = _levelSeed;
        int paletteIndex = (s ^ (s >> 8) ^ (s >> 16)) & 7;
        switch (paletteIndex)
        {
            case 0: _wallColorR = 220; _wallColorG = 220; _wallColorB = 220; break;
            case 1: _wallColorR = 200; _wallColorG = 140; _wallColorB = 120; break;
            case 2: _wallColorR = 140; _wallColorG = 190; _wallColorB = 210; break;
            case 3: _wallColorR = 170; _wallColorG = 200; _wallColorB = 140; break;
            case 4: _wallColorR = 210; _wallColorG = 180; _wallColorB = 120; break;
            case 5: _wallColorR = 150; _wallColorG = 150; _wallColorB = 210; break;
            case 6: _wallColorR = 190; _wallColorG = 160; _wallColorB = 200; break;
            default: _wallColorR = 200; _wallColorG = 200; _wallColorB = 160; break;
        }
    }

    private void Update(float dt)
    {
        if (Input.WasPressed(Key.F)) _flyMode = !_flyMode;
        if (Input.WasPressed(Key.M)) _minimapOn = !_minimapOn;

        if (Input.IsDown(Key.Left)) _yaw += LookSpeed * dt;
        if (Input.IsDown(Key.Right)) _yaw -= LookSpeed * dt;
        if (Input.IsDown(Key.Up)) _pitch -= LookSpeed * dt;
        if (Input.IsDown(Key.Down)) _pitch += LookSpeed * dt;

        if (_pitch > 1.5f) _pitch = 1.5f;
        if (_pitch < -1.5f) _pitch = -1.5f;

        float cy = MathF.Cos(_yaw);
        float sy = MathF.Sin(_yaw);
        float cp = MathF.Cos(_pitch);
        float sp = MathF.Sin(_pitch);

        float fx = _flyMode ? -sy * cp : -sy;
        float fy = _flyMode ? -sp : 0f;
        float fz = _flyMode ? cy * cp : cy;

        float rx = cy;
        float rz = sy;

        float prevX = _camX;
        float prevZ = _camZ;

        float speed = MoveSpeed * dt;
        if (Input.IsDown(Key.W))
        {
            _camX += fx * speed;
            _camY += fy * speed;
            _camZ += fz * speed;
        }
        if (Input.IsDown(Key.S))
        {
            _camX -= fx * speed;
            _camY -= fy * speed;
            _camZ -= fz * speed;
        }
        if (Input.IsDown(Key.A))
        {
            _camX -= rx * speed;
            _camZ -= rz * speed;
        }
        if (Input.IsDown(Key.D))
        {
            _camX += rx * speed;
            _camZ += rz * speed;
        }

        _camY = _wallH * EyeHeightRatio;
        if (_flyMode)
        {
            if (Input.IsDown(Key.Space)) _camY -= speed;
            if (Input.IsDown(Key.Shift)) _camY += speed;
        }

        if (!_spawned)
        {
            for (int z = 0; z < _rows && !_spawned; z++)
            {
                for (int x = 0; x < _cols && !_spawned; x++)
                {
                    if (!_maze.IsWall(x, z))
                    {
                        _camX = _startX + (x + 0.5f) * _cell;
                        _camZ = _startZ + (z + 0.5f) * _cell;
                        _spawned = true;
                    }
                }
            }
            prevX = _camX;
            prevZ = _camZ;
        }

        if (!_flyMode)
        {
            float testX = _camX;
            float testZ = _camZ;

            int cellX = (int)MathF.Floor((testX - _startX) / _cell);
            int cellZ = (int)MathF.Floor((testZ - _startZ) / _cell);

            if (IsWallCell(cellX, cellZ))
            {
                int cellXOnly = (int)MathF.Floor((testX - _startX) / _cell);
                int cellZPrev = (int)MathF.Floor((prevZ - _startZ) / _cell);
                if (IsWallCell(cellXOnly, cellZPrev)) testX = prevX;

                int cellXPrev = (int)MathF.Floor((prevX - _startX) / _cell);
                int cellZOnly = (int)MathF.Floor((testZ - _startZ) / _cell);
                if (IsWallCell(cellXPrev, cellZOnly)) testZ = prevZ;
            }

            _camX = testX;
            _camZ = testZ;
        }

        PlaceGoalIfNeeded();

        float goalDX = _goalX - _camX;
        float goalDZ = _goalZ - _camZ;
        float goalDist = MathF.Sqrt(goalDX * goalDX + goalDZ * goalDZ);
        if (goalDist < _cell * 0.35f)
        {
            _levelIndex += 1;
            _levelSeed = (int)(DateTime.UtcNow.Ticks % 1000000000L) + 1;
            BuildLevel();
        }
    }

    private bool IsWallCell(int mx, int mz)
    {
        if (mx < 0 || mz < 0 || mx >= _cols || mz >= _rows) return true;
        return _maze.IsWall(mx, mz);
    }

    private void PlaceGoalIfNeeded()
    {
        if (_goalPlaced && _goalLevelIndex == _levelIndex) return;

        int centerX = _cols / 2;
        int centerZ = _rows / 2;
        int gx = -1;
        int gz = -1;

        if (!_maze.IsWall(centerX, centerZ))
        {
            gx = centerX;
            gz = centerZ;
        }
        else
        {
            int maxR = _rows > _cols ? _rows : _cols;
            for (int r = 1; r < maxR && gx < 0; r++)
            {
                for (int dz = -r; dz <= r && gx < 0; dz++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dz) != r) continue;
                        int mx = centerX + dx;
                        int mz = centerZ + dz;
                        if (mx < 0 || mz < 0 || mx >= _cols || mz >= _rows) continue;
                        if (!_maze.IsWall(mx, mz))
                        {
                            gx = mx;
                            gz = mz;
                            break;
                        }
                    }
                }
            }
        }

        if (gx >= 0)
        {
            _goalX = _startX + (gx + 0.5f) * _cell;
            _goalZ = _startZ + (gz + 0.5f) * _cell;
        }

        _goalPlaced = true;
        _goalLevelIndex = _levelIndex;
    }

    private void Render()
    {
        _frame.Clear(20, 20, 25);

        float cy = MathF.Cos(_yaw);
        float sy = MathF.Sin(_yaw);

        float posX = (_camX - _startX) / _cell;
        float posZ = (_camZ - _startZ) / _cell;

        float fovRad = MathF.PI / 3f;
        float planeScale = MathF.Tan(fovRad * 0.5f);
        float projScale = (ScreenW * 0.5f) / planeScale;
        float forwardX = -sy;
        float forwardZ = cy;
        float rightX = cy;
        float rightZ = sy;
        float planeX = rightX * planeScale;
        float planeZ = rightZ * planeScale;

        int rayStep = 2;
        float yOffset = -_pitch * (ScreenH * 0.5f);

        for (int i = 0; i < _wallDepth.Length; i++) _wallDepth[i] = 1e9f;

        for (int sx = 0; sx < ScreenW; sx += rayStep)
        {
            float cameraX = (2f * sx / ScreenW) - 1f;
            float rayDirX = forwardX + planeX * cameraX;
            float rayDirZ = forwardZ + planeZ * cameraX;

            int mapX = (int)MathF.Floor(posX);
            int mapZ = (int)MathF.Floor(posZ);

            float deltaDistX = MathF.Abs(1f / rayDirX);
            float deltaDistZ = MathF.Abs(1f / rayDirZ);

            int stepX;
            int stepZ;
            float sideDistX;
            float sideDistZ;

            if (rayDirX < 0)
            {
                stepX = -1;
                sideDistX = (posX - mapX) * deltaDistX;
            }
            else
            {
                stepX = 1;
                sideDistX = (mapX + 1f - posX) * deltaDistX;
            }

            if (rayDirZ < 0)
            {
                stepZ = -1;
                sideDistZ = (posZ - mapZ) * deltaDistZ;
            }
            else
            {
                stepZ = 1;
                sideDistZ = (mapZ + 1f - posZ) * deltaDistZ;
            }

            bool hit = false;
            int side = 0;
            int safety = 0;

            while (!hit && safety < 64)
            {
                safety++;
                if (sideDistX < sideDistZ)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDistZ += deltaDistZ;
                    mapZ += stepZ;
                    side = 1;
                }

                if (IsWallCell(mapX, mapZ)) hit = true;
            }

            if (!hit) continue;

            float perpDist;
            if (side == 0)
            {
                perpDist = (mapX - posX + (1 - stepX) * 0.5f) / rayDirX;
            }
            else
            {
                perpDist = (mapZ - posZ + (1 - stepZ) * 0.5f) / rayDirZ;
            }

            float worldDist = perpDist * _cell;
            if (worldDist < 1f) worldDist = 1f;

            float lineH = (_wallH * projScale) / worldDist;
            int top = (int)(ScreenH * 0.5f - lineH * 0.5f + yOffset);
            int bottom = (int)(ScreenH * 0.5f + lineH * 0.5f + yOffset);

            byte shade = (byte)(255 - MathF.Min(220f, worldDist * 0.6f));
            float sideMul = side == 0 ? 1f : 0.85f;
            byte r = (byte)(MathF.Min(255f, shade * (_wallColorR / 255f) * sideMul));
            byte g = (byte)(MathF.Min(255f, shade * (_wallColorG / 255f) * sideMul));
            byte b = (byte)(MathF.Min(255f, shade * (_wallColorB / 255f) * sideMul));

            _frame.DrawColumn(sx + rayStep / 2, top, bottom, rayStep, r, g, b);

            int fillEnd = sx + rayStep;
            if (fillEnd > ScreenW) fillEnd = ScreenW;
            for (int fillX = sx; fillX < fillEnd; fillX++)
            {
                _wallDepth[fillX] = worldDist;
            }
        }

        DrawGoalFloor(forwardX, forwardZ, planeX, planeZ, posX, posZ, yOffset);

        if (_minimapOn) DrawMinimap(forwardX, forwardZ, posX, posZ);
    }

    private void DrawGoalFloor(float forwardX, float forwardZ, float planeX, float planeZ, float posX, float posZ, float yOffset)
    {
        int goalCellX = (int)((_goalX - _startX) / _cell);
        int goalCellZ = (int)((_goalZ - _startZ) / _cell);

        float rayDirX0 = forwardX - planeX;
        float rayDirZ0 = forwardZ - planeZ;
        float rayDirX1 = forwardX + planeX;
        float rayDirZ1 = forwardZ + planeZ;

        int halfH = ScreenH / 2;
        int horizon = (int)(halfH + yOffset);
        float camHeight = (_wallH * EyeHeightRatio) / _cell;

        for (int y = horizon; y < ScreenH; y++)
        {
            int p = y - horizon;
            if (p <= 0) continue;

            float rowDist = (camHeight * halfH) / p;
            float floorStepX = rowDist * (rayDirX1 - rayDirX0) / ScreenW;
            float floorStepZ = rowDist * (rayDirZ1 - rayDirZ0) / ScreenW;
            float floorX = posX + rowDist * rayDirX0;
            float floorZ = posZ + rowDist * rayDirZ0;
            float worldDist = rowDist * _cell;

            for (int x = 0; x < ScreenW; x++)
            {
                if (worldDist < _wallDepth[x])
                {
                    int cellX = (int)floorX;
                    int cellZ = (int)floorZ;
                    if (cellX == goalCellX && cellZ == goalCellZ)
                    {
                        _frame.SetPixel(x, y, 20, 220, 80);
                    }
                    else
                    {
                        byte shade = (byte)(20 + MathF.Min(90f, worldDist * 0.35f));
                        _frame.SetPixel(x, y, shade, shade, shade);
                    }
                }

                floorX += floorStepX;
                floorZ += floorStepZ;
            }
        }
    }

    private void DrawMinimap(float forwardX, float forwardZ, float posX, float posZ)
    {
        int mapScale = 8;
        int mapPad = 10;
        int mapW = _cols * mapScale;
        int mapH = _rows * mapScale;

        _frame.DrawRect(mapPad, mapPad, mapW, mapH, 240, 240, 240);

        for (int mz = 0; mz < _rows; mz++)
        {
            for (int mx = 0; mx < _cols; mx++)
            {
                if (_maze.IsWall(mx, mz))
                {
                    int drawY = mapPad + (_rows - 1 - mz) * mapScale;
                    _frame.DrawRect(mapPad + mx * mapScale, drawY, mapScale, mapScale, 20, 20, 20);
                }
            }
        }

        int pX = mapPad + (int)(posX * mapScale - mapScale * 0.25f);
        int pY = mapPad + (int)((_rows - posZ) * mapScale - mapScale * 0.25f);
        _frame.DrawRect(pX, pY, mapScale / 2, mapScale / 2, 200, 40, 40);

        float dirLen = mapScale * 0.6f;
        float fxMap = forwardX * dirLen;
        float fzMap = forwardZ * dirLen;
        int dX = mapPad + (int)(posX * mapScale + fxMap - mapScale * 0.15f);
        int dY = mapPad + (int)((_rows - posZ) * mapScale - fzMap - mapScale * 0.15f);
        _frame.DrawRect(dX, dY, mapScale / 3, mapScale / 3, 200, 40, 40);

        int gMapX = mapPad + (int)((_goalX - _startX) / _cell * mapScale - mapScale * 0.25f);
        int gMapY = mapPad + (int)((_rows - (_goalZ - _startZ) / _cell) * mapScale - mapScale * 0.25f);
        _frame.DrawRect(gMapX, gMapY, mapScale / 2, mapScale / 2, 20, 220, 80);
    }
}
