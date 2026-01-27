using System;
using System.Runtime.InteropServices;

namespace CosmosMaze.Platform.Mac;

internal static class ObjC
{
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    public static extern IntPtr GetClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    public static extern IntPtr SelRegisterName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_allocateClassPair")]
    public static extern IntPtr AllocateClassPair(IntPtr superclass, string name, int extraBytes);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_registerClassPair")]
    public static extern void RegisterClassPair(IntPtr cls);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "class_addMethod")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool AddMethod(IntPtr cls, IntPtr sel, Delegate imp, string types);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_Bool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool arg1);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_UInt64(IntPtr receiver, IntPtr selector, ulong arg1);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern ushort MsgSend_UInt16(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern void MsgSend_Void(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern void MsgSend_Void_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern void MsgSend_Void_Bool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool arg1);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern void MsgSend_Void_UInt64(IntPtr receiver, IntPtr selector, ulong arg1);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_IntPtr_UInt64(IntPtr receiver, IntPtr selector, ulong arg1);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_CGRect(IntPtr receiver, IntPtr selector, CGRect rect);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_CGRect_UInt64_UInt64_Bool(IntPtr receiver, IntPtr selector, CGRect rect, ulong styleMask, ulong backing, [MarshalAs(UnmanagedType.I1)] bool defer);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    public static extern IntPtr MsgSend_Double_IntPtr_IntPtr_IntPtr_Bool(IntPtr receiver, IntPtr selector, double interval, IntPtr target, IntPtr sel, IntPtr userInfo, [MarshalAs(UnmanagedType.I1)] bool repeats);

    [StructLayout(LayoutKind.Sequential)]
    public struct CGRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public CGRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CGSize
    {
        public double Width;
        public double Height;

        public CGSize(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CGPoint
    {
        public double X;
        public double Y;

        public CGPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
