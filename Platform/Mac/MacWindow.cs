using System;
using System.Runtime.InteropServices;
using CosmosMaze.Core;

namespace CosmosMaze.Platform.Mac;

internal sealed class MacWindow : IPlatformWindow
{
    private const ulong WindowStyleTitled = 1 << 0;
    private const ulong WindowStyleClosable = 1 << 1;
    private const ulong WindowStyleMiniaturizable = 1 << 2;
    private const ulong WindowStyleResizable = 1 << 3;

    private readonly Game _game;
    private readonly FrameBuffer _frame;
    private GCHandle _bufferHandle;
    private IntPtr _bufferPtr;

    private IntPtr _app;
    private IntPtr _window;
    private IntPtr _view;
    private IntPtr _timerTarget;

    private static MacWindow _instance = null!;
    private static IntPtr _viewClass;
    private static IntPtr _timerTargetClass;
    private static IntPtr _sigVoidObj;

    private static readonly ObjC.CGRect ViewRect = new ObjC.CGRect(0, 0, Game.ScreenW, Game.ScreenH);

    private static readonly DrawRectDelegate DrawRectImp = DrawRect;
    private static readonly KeyEventDelegate KeyDownImp = KeyDown;
    private static readonly KeyEventDelegate KeyUpImp = KeyUp;
    private static readonly BoolDelegate AcceptsFirstResponderImp = AcceptsFirstResponder;
    private static readonly BoolDelegate IsFlippedImp = IsFlipped;
    private static readonly TimerDelegate TickImp = Tick;
    private static readonly MethodSignatureDelegate MethodSignatureImp = MethodSignatureForSelector;
    private static readonly ForwardInvocationDelegate ForwardInvocationImp = ForwardInvocation;

    public MacWindow(Game game)
    {
        _game = game;
        _frame = game.Frame;
    }

    public FrameBuffer Frame => _frame;

    public void Run()
    {
        _instance = this;
        _bufferHandle = GCHandle.Alloc(_frame.Pixels, GCHandleType.Pinned);
        _bufferPtr = _bufferHandle.AddrOfPinnedObject();

        SetupApp();
        SetupWindow();
        SetupTimer();

        ObjC.MsgSend_Void(_app, ObjC.SelRegisterName("finishLaunching"));

        ObjC.MsgSend_Void(_app, ObjC.SelRegisterName("run"));
    }

    private void SetupApp()
    {
        Native.LoadFramework("/System/Library/Frameworks/Foundation.framework/Foundation");
        Native.LoadFramework("/System/Library/Frameworks/AppKit.framework/AppKit");

        IntPtr nsAppClass = ObjC.GetClass("NSApplication");
        _app = ObjC.MsgSend(nsAppClass, ObjC.SelRegisterName("sharedApplication"));
        ObjC.MsgSend_Void_UInt64(_app, ObjC.SelRegisterName("setActivationPolicy:"), 0);
        ObjC.MsgSend_Void_Bool(_app, ObjC.SelRegisterName("activateIgnoringOtherApps:"), true);
    }

    private void SetupWindow()
    {
        if (_viewClass == IntPtr.Zero) RegisterViewClass();

        IntPtr nsWindowClass = ObjC.GetClass("NSWindow");
        IntPtr windowAlloc = ObjC.MsgSend(nsWindowClass, ObjC.SelRegisterName("alloc"));
        ulong styleMask = WindowStyleTitled | WindowStyleClosable | WindowStyleMiniaturizable | WindowStyleResizable;
        _window = ObjC.MsgSend_CGRect_UInt64_UInt64_Bool(windowAlloc, ObjC.SelRegisterName("initWithContentRect:styleMask:backing:defer:"), ViewRect, styleMask, 2, false);

        IntPtr viewAlloc = ObjC.MsgSend(_viewClass, ObjC.SelRegisterName("alloc"));
        _view = ObjC.MsgSend_CGRect(viewAlloc, ObjC.SelRegisterName("initWithFrame:"), ViewRect);

        ObjC.MsgSend_Void_IntPtr(_window, ObjC.SelRegisterName("setContentView:"), _view);
        ObjC.MsgSend_Void_IntPtr(_window, ObjC.SelRegisterName("makeFirstResponder:"), _view);

        IntPtr title = ToNSString("CosmosMaze");
        ObjC.MsgSend_Void_IntPtr(_window, ObjC.SelRegisterName("setTitle:"), title);

        ObjC.MsgSend_Void_IntPtr(_window, ObjC.SelRegisterName("makeKeyAndOrderFront:"), IntPtr.Zero);
    }

    private void SetupTimer()
    {
        if (_timerTargetClass == IntPtr.Zero) RegisterTimerTargetClass();

        IntPtr targetAlloc = ObjC.MsgSend(_timerTargetClass, ObjC.SelRegisterName("alloc"));
        _timerTarget = ObjC.MsgSend(targetAlloc, ObjC.SelRegisterName("init"));

        IntPtr nsTimerClass = ObjC.GetClass("NSTimer");
        ObjC.MsgSend_Double_IntPtr_IntPtr_IntPtr_Bool(
            nsTimerClass,
            ObjC.SelRegisterName("scheduledTimerWithTimeInterval:target:selector:userInfo:repeats:"),
            1.0 / 60.0,
            _timerTarget,
            ObjC.SelRegisterName("tick:"),
            IntPtr.Zero,
            true);
    }

    private static void RegisterViewClass()
    {
        IntPtr nsViewClass = ObjC.GetClass("NSView");
        _viewClass = ObjC.AllocateClassPair(nsViewClass, "CosmosMazeView", 0);

        ObjC.AddMethod(_viewClass, ObjC.SelRegisterName("drawRect:"), DrawRectImp, "v@:{CGRect={CGPoint=dd}{CGSize=dd}}");
        ObjC.AddMethod(_viewClass, ObjC.SelRegisterName("keyDown:"), KeyDownImp, "v@:@");
        ObjC.AddMethod(_viewClass, ObjC.SelRegisterName("keyUp:"), KeyUpImp, "v@:@");
        ObjC.AddMethod(_viewClass, ObjC.SelRegisterName("acceptsFirstResponder"), AcceptsFirstResponderImp, "B@:");
        ObjC.AddMethod(_viewClass, ObjC.SelRegisterName("isFlipped"), IsFlippedImp, "B@:");
        ObjC.AddMethod(_viewClass, ObjC.SelRegisterName("methodSignatureForSelector:"), MethodSignatureImp, "@@::");
        ObjC.AddMethod(_viewClass, ObjC.SelRegisterName("forwardInvocation:"), ForwardInvocationImp, "v@:@");

        ObjC.RegisterClassPair(_viewClass);
    }

    private static void RegisterTimerTargetClass()
    {
        IntPtr nsObjectClass = ObjC.GetClass("NSObject");
        _timerTargetClass = ObjC.AllocateClassPair(nsObjectClass, "CosmosMazeTimerTarget", 0);
        ObjC.AddMethod(_timerTargetClass, ObjC.SelRegisterName("tick:"), TickImp, "v@:@");
        ObjC.RegisterClassPair(_timerTargetClass);
    }

    private static IntPtr ToNSString(string value)
    {
        IntPtr nsStringClass = ObjC.GetClass("NSString");
        IntPtr sel = ObjC.SelRegisterName("stringWithUTF8String:");
        IntPtr utf8 = Marshal.StringToHGlobalAnsi(value);
        IntPtr str = ObjC.MsgSend_IntPtr(nsStringClass, sel, utf8);
        Marshal.FreeHGlobal(utf8);
        return str;
    }

    private static void DrawRect(IntPtr self, IntPtr cmd, ObjC.CGRect rect)
    {
        if (_instance == null) return;

        IntPtr nsGraphicsContextClass = ObjC.GetClass("NSGraphicsContext");
        IntPtr ctx = ObjC.MsgSend(nsGraphicsContextClass, ObjC.SelRegisterName("currentContext"));
        IntPtr cgContext = ObjC.MsgSend(ctx, ObjC.SelRegisterName("graphicsPort"));

        IntPtr colorSpace = CoreGraphics.CGColorSpaceCreateDeviceRGB();
        IntPtr provider = CoreGraphics.CGDataProviderCreateWithData(IntPtr.Zero, _instance._bufferPtr, (IntPtr)(_instance._frame.Pixels.Length), IntPtr.Zero);
        uint bitmapInfo = CoreGraphics.kCGImageAlphaPremultipliedLast | CoreGraphics.kCGBitmapByteOrder32Big;
        IntPtr image = CoreGraphics.CGImageCreate(_instance._frame.Width, _instance._frame.Height, 8, 32, _instance._frame.Width * 4, colorSpace, bitmapInfo, provider, IntPtr.Zero, false, 0);

        CoreGraphics.CGContextSaveGState(cgContext);
        CoreGraphics.CGContextTranslateCTM(cgContext, 0, _instance._frame.Height);
        CoreGraphics.CGContextScaleCTM(cgContext, 1, -1);
        CoreGraphics.CGContextDrawImage(cgContext, ViewRect, image);
        CoreGraphics.CGContextRestoreGState(cgContext);

        CoreGraphics.CGImageRelease(image);
        CoreGraphics.CGDataProviderRelease(provider);
        CoreGraphics.CGColorSpaceRelease(colorSpace);
    }

    private static void KeyDown(IntPtr self, IntPtr cmd, IntPtr evt)
    {
        ushort keyCode = ObjC.MsgSend_UInt16(evt, ObjC.SelRegisterName("keyCode"));
        SetKeyState(keyCode, true);
    }

    private static void KeyUp(IntPtr self, IntPtr cmd, IntPtr evt)
    {
        ushort keyCode = ObjC.MsgSend_UInt16(evt, ObjC.SelRegisterName("keyCode"));
        SetKeyState(keyCode, false);
    }

    private static void SetKeyState(ushort keyCode, bool down)
    {
        switch (keyCode)
        {
            case 13: Input.SetKey(Key.W, down); break;
            case 0: Input.SetKey(Key.A, down); break;
            case 1: Input.SetKey(Key.S, down); break;
            case 2: Input.SetKey(Key.D, down); break;
            case 123: Input.SetKey(Key.Left, down); break;
            case 124: Input.SetKey(Key.Right, down); break;
            case 126: Input.SetKey(Key.Up, down); break;
            case 125: Input.SetKey(Key.Down, down); break;
            case 49: Input.SetKey(Key.Space, down); break;
            case 56:
            case 60:
                Input.SetKey(Key.Shift, down);
                break;
            case 3: Input.SetKey(Key.F, down); break;
            case 46: Input.SetKey(Key.M, down); break;
        }
    }

    private static bool AcceptsFirstResponder(IntPtr self, IntPtr cmd)
    {
        return true;
    }

    private static bool IsFlipped(IntPtr self, IntPtr cmd)
    {
        return true;
    }

    private static IntPtr MethodSignatureForSelector(IntPtr self, IntPtr cmd, IntPtr sel)
    {
        if (_sigVoidObj == IntPtr.Zero)
        {
            IntPtr nsMethodSignatureClass = ObjC.GetClass("NSMethodSignature");
            IntPtr typeStr = Marshal.StringToHGlobalAnsi("v@:@");
            _sigVoidObj = ObjC.MsgSend_IntPtr_IntPtr(nsMethodSignatureClass, ObjC.SelRegisterName("signatureWithObjCTypes:"), typeStr);
            Marshal.FreeHGlobal(typeStr);
        }
        return _sigVoidObj;
    }

    private static void ForwardInvocation(IntPtr self, IntPtr cmd, IntPtr invocation)
    {
    }

    private static void Tick(IntPtr self, IntPtr cmd, IntPtr timer)
    {
        if (_instance == null) return;
        _instance._game.Tick(1f / 60f);
        ObjC.MsgSend_Void_Bool(_instance._view, ObjC.SelRegisterName("setNeedsDisplay:"), true);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DrawRectDelegate(IntPtr self, IntPtr cmd, ObjC.CGRect rect);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void KeyEventDelegate(IntPtr self, IntPtr cmd, IntPtr evt);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool BoolDelegate(IntPtr self, IntPtr cmd);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TimerDelegate(IntPtr self, IntPtr cmd, IntPtr timer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MethodSignatureDelegate(IntPtr self, IntPtr cmd, IntPtr sel);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ForwardInvocationDelegate(IntPtr self, IntPtr cmd, IntPtr invocation);
}
