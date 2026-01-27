namespace CosmosMaze.Core;

internal enum Key
{
    W,
    A,
    S,
    D,
    Left,
    Right,
    Up,
    Down,
    Space,
    Shift,
    F,
    M,
    Count
}

internal static class Input
{
    private static readonly bool[] Down = new bool[(int)Key.Count];
    private static readonly bool[] Pressed = new bool[(int)Key.Count];

    public static void SetKey(Key key, bool isDown)
    {
        int idx = (int)key;
        if (isDown && !Down[idx])
        {
            Pressed[idx] = true;
        }
        Down[idx] = isDown;
    }

    public static bool IsDown(Key key)
    {
        return Down[(int)key];
    }

    public static bool WasPressed(Key key)
    {
        int idx = (int)key;
        bool pressed = Pressed[idx];
        Pressed[idx] = false;
        return pressed;
    }
}
