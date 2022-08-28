using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace xasset.editor
{
    /// <summary>
    /// 加密文件
    /// </summary>
    public class EncryptFile : BuildTaskJob
    {
        public EncryptFile(BuildTask task) : base(task)
        {
        }

        protected override void DoTask()
        {
            Debug.Log($"是否开启文件加密模式 : {Settings.EncryptionEnabled}");
            Debug.Log($"加密忽略文件后缀名： {String.Join(",", Settings.EncryptionExcludeFiles)}");

            if (!Settings.EncryptionEnabled) return;

            var bundles = _task.bundles;
            for(var index = 0; index < bundles.Count; ++index)
            {
                var bundle = bundles[index];
                var path = GetBuildPath(bundle.nameWithAppendHash);
                if (bundle.isRaw && Settings.IsEncryptionExcluded(path)) continue;
                
                Encrypt(bundle, path);

                var fileExtension = Path.GetExtension(path);
                var hash = Utility.ComputeHash(path);
                var nameWithAppendHash = $"{bundle.name}_{hash}{fileExtension}";
                var newPath = GetBuildPath(nameWithAppendHash);
                
                // 加密后覆盖原文件
                if (!File.Exists(newPath))
                {
                    File.Move(path, newPath);
                }
                else if(bundle.hash != hash)
                {
                    File.Delete(path);
                }
                
                bundle.hash = hash;
                bundle.nameWithAppendHash = nameWithAppendHash;

                UnityEditor.EditorUtility.DisplayProgressBar("Encrypt File: ", bundle.name,
                    (index + 1) / (float)bundles.Count);
            }
            UnityEditor.EditorUtility.ClearProgressBar();
        }
        
        /// <summary>
        /// 加密
        /// </summary>
        private static void Encrypt(ManifestBundle bundle, string filePath)
        {
            if (!bundle.isRaw)
            {
                var uniqueSalt = Encoding.UTF8.GetBytes(bundle.name);
                var data = File.ReadAllBytes(filePath);
                using (var baseStream = new FileStream(filePath, FileMode.Open))
                {
                    var cipher = new SeekableAesStream(baseStream, Utility.EncryptKey, uniqueSalt);
                    cipher.Write(data, 0, data.Length);
                }
            }
            else
            {
                Utility.EncryptBinaryFile(filePath);
            }
        }
    }
}