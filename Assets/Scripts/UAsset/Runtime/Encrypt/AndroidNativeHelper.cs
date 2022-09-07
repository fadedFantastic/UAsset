using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UAsset
{
    public static class AndroidNativeHelper
    {
#if UNITY_ANDROID
        private const string libName = "nativehelper";
        
        [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NativeInit();

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
        private static extern int ReadAllBytes(string fileName, IntPtr result);
        
        
        private static readonly string SplitFlag = "!/assets/";
        private static readonly int SplitFlagLength = SplitFlag.Length;
        
        static AndroidNativeHelper()
        {
            NativeInit();
        }
        
        public static void ReadAllBytes(string filePath, ref byte[] buffer)
        {
            filePath = GetAndroidFilePath(filePath);
            IntPtr ptr = Marshal.AllocHGlobal(buffer.Length);
            try
            {
                var size = ReadAllBytes(filePath, ptr);
                if (size > 0)
                {
                    if (ptr == IntPtr.Zero)
                    {
                        throw new Exception("Read Failed");
                    }
                    
                    Debug.Log($"ReadAllBytes size: {size}");
                    Marshal.Copy(ptr, buffer, 0, size);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        
        public static string GetAndroidFilePath(string filePath)
        {
            var position = filePath.LastIndexOf(SplitFlag, StringComparison.Ordinal);
            if (position < 0)
            {
                throw new Exception("Can not find split flag in full path.");
            }
            
            filePath = filePath.Substring(position + SplitFlagLength);
            return filePath;
        }
        
#endif
    }
}