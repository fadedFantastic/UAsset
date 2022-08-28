#if UNITY_ANDROID
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace xasset
{
    /// <summary>
    /// 读取Android的assets目录下的文件流
    /// </summary>
    public class AndroidFileStream : Stream
    {
        private static readonly string SplitFlag = "!/assets/";
        private static readonly int SplitFlagLength = SplitFlag.Length;
        private readonly IntPtr m_FileStreamRawObject;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => GetLength();
        public override long Position { get; set; }
        
        static AndroidFileStream()
        {
            AndroidNativeHelper.NativeInit();
        }

        /// <summary>
        /// 初始化安卓文件系统流的新实例。
        /// </summary>
        /// <param name="fullPath">要加载的文件系统的完整路径。</param>
        public AndroidFileStream(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new Exception("Full path is invalid.");
            }

            int position = fullPath.LastIndexOf(SplitFlag, StringComparison.Ordinal);
            if (position < 0)
            {
                throw new Exception("Can not find split flag in full path.");
            }

            string fileName = fullPath.Substring(position + SplitFlagLength);
            m_FileStreamRawObject = InternalOpen(fileName);
            
            if (m_FileStreamRawObject == null)
            {
                throw new Exception($"Open file '{fullPath}' from Android asset manager failure.");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int size;
            IntPtr ptr = Marshal.AllocHGlobal(buffer.Length);
            try
            {
                size = AndroidNativeHelper.Read(m_FileStreamRawObject, ptr, count);
                if (size > 0)
                {
                    if (ptr == IntPtr.Zero)
                    {
                        throw new Exception("Read Failed");
                    }

                    Marshal.Copy(ptr, buffer, offset, size);
                    Position += size;
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

            return size;
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            var off = AndroidNativeHelper.Seek(m_FileStreamRawObject, offset, (int)origin);
            if (off > 0)
            {
                Position = off;
            }
            return Position;
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new Exception("WriteByte is not supported in AndroidFileSystemStream.");
        }

        public override void Flush()
        {
            throw new Exception("Flush is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 设置文件系统流长度。
        /// </summary>
        /// <param name="length">要设置的文件系统流的长度。</param>
        public override void SetLength(long length)
        {
            throw new Exception("SetLength is not supported in AndroidFileSystemStream.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                InternalClose();
            }
            
            base.Dispose(disposing);
        }
        
        private IntPtr InternalOpen(string fileName)
        {
            return AndroidNativeHelper.Open(fileName);
        }

        private void InternalClose()
        {
            AndroidNativeHelper.Close(m_FileStreamRawObject);
        }

        private long GetLength()
        {
            return AndroidNativeHelper.GetLength(m_FileStreamRawObject);
        }
    }
}
#endif