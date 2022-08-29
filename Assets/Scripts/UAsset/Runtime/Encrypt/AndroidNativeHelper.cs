using System;
using System.Runtime.InteropServices;

namespace UAsset
{
    public class AndroidNativeHelper
    {
        public const string libName = "xlua";

#if UNITY_ANDROID

        [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void NativeInit();

        [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Open(string fileName);

        [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Close(IntPtr asset);


        [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Read(IntPtr asset, IntPtr b, int count);

        [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long Seek(IntPtr asset, long offset, int whence);
        
        [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
        public static extern long GetLength(IntPtr asset);
#endif
    }
}