using System.Runtime.InteropServices;
using UnityEngine;

namespace xasset
{
    public class DiskUtility
    {
#if UNITY_ANDROID
        private static AndroidJavaClass m_DiskUtilsClass;
        
        static DiskUtility()
        {
            m_DiskUtilsClass = new AndroidJavaClass("DiskUtils");
        }

#elif UNITY_IOS

        [DllImport("__Internal")]
        private static extern long getAvailableDiskSpace();
#endif
        
        /// <summary>
        /// 获取设备磁盘大小
        /// </summary>
        /// <returns></returns>
        public static long GetAvailableSpace()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            return long.MaxValue;
#elif UNITY_ANDROID
            return m_DiskUtilsClass.CallStatic<long>("availableSpace", false);
#elif UNITY_IOS
            return getAvailableDiskSpace();
#endif
        }
    }
}