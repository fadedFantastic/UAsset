using System;
using System.Runtime.InteropServices;

namespace UAsset
{
    public class AndroidNativeHelper
    {
        public const string libName = "nativehelper";

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
        
        [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReadAllBytes(string fileName, ref IntPtr result);
        
        
        public static void ReadAllBytes(string fileName, ref byte[] buffer)
        {
            IntPtr ptr = Marshal.AllocHGlobal(buffer.Length);
            try
            {
                var size = ReadAllBytes(fileName, ref ptr);
                if (size > 0)
                {
                    if (ptr == IntPtr.Zero)
                    {
                        throw new Exception("Read Failed");
                    }

                    Marshal.Copy(ptr, buffer, 0, size);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        
#endif
    }
}