using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UAsset
{
    public static class Utility
    {
        public const string nonsupport = "Nonsupport";
        
        public static string BuildPath { get; set; } = "Bundles";

        public static string EncryptKey => "UASSET";

        private static readonly double[] byteUnits =
        {
            1073741824.0, 1048576.0, 1024.0, 1
        };

        private static readonly string[] byteUnitsNames =
        {
            "GB", "MB", "KB", "B"
        };

        /// <summary>
        /// 获取对应平台名称
        /// </summary>
        public static string GetPlatformName(RuntimePlatform? platform = null)
        {
            platform = platform ?? Application.platform;
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.OSXPlayer:
                    return "MacOS";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                default:
                    return nonsupport;
            }
        }

        public static string FormatBytes(long bytes)
        {
            var size = "0 B";
            if (bytes == 0) return size;

            for (var index = 0; index < byteUnits.Length; index++)
            {
                var unit = byteUnits[index];
                if (!(bytes >= unit)) continue;

                size = $"{bytes / unit:##.##} {byteUnitsNames[index]}";
                break;
            }

            return size;
        }
        
        /// <summary>
        /// 从Json中获得ScriptableObject对象
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <typeparam name="T">ScriptableObject类型</typeparam>
        /// <returns></returns>
        public static T LoadScriptableObjectWithJson<T>(string filename) where T : ScriptableObject
        {
            if (!File.Exists(filename))
            {
                return ScriptableObject.CreateInstance<T>();
            }

            var json = ReadFile(filename);
            var asset = ScriptableObject.CreateInstance<T>();
            try
            {
                JsonUtility.FromJsonOverwrite(json, asset);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                File.Delete(filename);
            }

            return asset;
        }
        
        #region 计算Hash相关

        public static string ToHash(IEnumerable<byte> data)
        {
            var sb = new StringBuilder();
            foreach (var t in data) sb.Append(t.ToString("x2"));

            return sb.ToString();
        }

        /// <summary>
        /// 计算hash值
        /// </summary>
        /// <param name="bytes">二进制数据</param>
        /// <returns></returns>
        public static string ComputeHash(byte[] bytes)
        {
            var data = MD5.Create().ComputeHash(bytes);
            return ToHash(data);
        }

        /// <summary>
        /// 计算文件hash值
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns></returns>
        public static string ComputeHash(string filename)
        {
            if (!File.Exists(filename)) return string.Empty;

            using (var stream = File.OpenRead(filename))
            {
                return ToHash(MD5.Create().ComputeHash(stream));
            }
        }
        
        public static string ComputeHash(Stream stream)
        {
            var buffer = new byte[32768]; // 32 kb
            var amount = (int) (stream.Length - stream.Position);
            using (var hashAlgorithm = MD5.Create())
            {
                while (amount > 0)
                {
                    var bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, amount));
                    if (bytesRead <= 0) continue;
                    amount -= bytesRead;
                    if (amount > 0)
                        hashAlgorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                    else
                        hashAlgorithm.TransformFinalBlock(buffer, 0, bytesRead);
                }

                return ToHash(hashAlgorithm.Hash);
            }
        }
        
        #endregion

        #region 文件操作相关
        
        /// <summary>
        /// 创建文件的文件夹路径
        /// </summary>
        public static void CreateFileDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            CreateDirectory(directory);
        }

        /// <summary>
        /// 创建文件夹路径
        /// </summary>
        public static void CreateDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static string ReadFile(string filePath)
        {
            return File.Exists(filePath) 
                ? File.ReadAllText(filePath)
                : string.Empty;
        }
        
        #endregion

        #region 加密相关

        /// <summary>
        /// 加密二进制文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void EncryptBinaryFile(string filePath)
        {
            var targetFile = File.ReadAllBytes(filePath);
            var fileLength = targetFile.Length;
            var keyBytes = Encoding.UTF8.GetBytes(EncryptKey);
            for (var i = 0; i < fileLength; ++i)
            {
                targetFile[i] = (byte)(targetFile[i] ^ keyBytes[i % keyBytes.Length]);
            }
            
            File.WriteAllBytes(filePath, targetFile);
        }

        /// <summary>
        /// 解密二进制文件
        /// </summary>
        /// <param name="encryptedFile">加密的二进制数组</param>
        /// <returns></returns>
        public static void DeEncryptBinaryFile(byte[] encryptedFile)
        {
            var fileLength = encryptedFile.Length;
            var keyBytes = Encoding.UTF8.GetBytes(EncryptKey);
            for (var i = 0; i < fileLength; ++i)
            {
                encryptedFile[i] = (byte)(encryptedFile[i] ^ keyBytes[i % keyBytes.Length]);
            }
        }
        
        #endregion
    }
}