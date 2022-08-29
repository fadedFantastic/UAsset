using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    public class DecryptBundleTool : EditorWindow
    {
        static DecryptBundleTool _thisInstance;
        
        private string _bundlesPath = string.Empty;
        private string _saveDir = string.Empty;
        private string _destinationPath = string.Empty;
        private string _decryptDirName = "Decrypt";

        [MenuItem(MenuItems.kBuildPreix + "解密bundle包工具", false, 55)]
        static void ShowWindow()
        {
            if (_thisInstance == null)
            {
                _thisInstance = GetWindow(typeof(DecryptBundleTool), false, "解密bundle包", true) as DecryptBundleTool;
                _thisInstance.minSize = new Vector2(600, 400);
            }
            _thisInstance.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选择需解密的bundle包根路径", GUILayout.MaxWidth(200)))
            {
                string resultPath = UnityEditor.EditorUtility.OpenFolderPanel("Select", "Bundles", "");
                if (string.IsNullOrEmpty(resultPath))
                    return;
                _bundlesPath = resultPath;
            }
            EditorGUILayout.LabelField(_bundlesPath);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选择生成解密后bundle包的目录", GUILayout.MaxWidth(200)))
            {
                string resultPath = UnityEditor.EditorUtility.OpenFolderPanel("Select", "Bundles", "");
                if (string.IsNullOrEmpty(resultPath))
                    return;
                _saveDir = resultPath;
            }
            EditorGUILayout.LabelField(_saveDir);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            if (GUILayout.Button("一键解密bundle", GUILayout.Width(150), GUILayout.Height(30)))
            {
                if (!string.IsNullOrEmpty(_bundlesPath) && !string.IsNullOrEmpty(_bundlesPath))
                {
                    Decrypt();
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("错误", "请先选择路径", "明白");
                }
            }
        }

        private void Decrypt()
        {
            var versionPath = GetSourcePath(Versions.Filename);
            if (!File.Exists(versionPath))
            {
                UnityEditor.EditorUtility.DisplayDialog("错误", "bundle包根路径选择不正确，不存在versionInfo.json版本文件", "明白");
                return;
            }
            
            var versions = BuildVersions.Load(versionPath);
            if (!versions.encryptionEnabled)
            {
                UnityEditor.EditorUtility.DisplayDialog("错误", "该路径下已经是未加密bundle", "明白");
                return;
            }

            // 构建解密存储路径
            var versionDir = $"{versions.gameVersion.Replace('.', '_')}_{versions.internalResourceVersion}";
            _destinationPath = $"{_saveDir}/{_decryptDirName}/{versionDir}";

            if (Directory.Exists(_destinationPath))
            {    
                Directory.Delete(_destinationPath, true);
            }

            var bundles = GetBuildBundles(versions);
            for (var index = 0; index < bundles.Count; index++)
            {
                var bundle = bundles[index];
                var path = GetSourcePath(bundle.nameWithAppendHash);
                // 线上包可能有些资源不存在
                if (!File.Exists(path)) continue;
                
                var destinationPath = GetSavePath(bundle.nameWithAppendHash);
                Copy(path, destinationPath);
                
                if (bundle.isRaw && Settings.IsEncryptionExcluded(path)) continue;

                DoDecrypt(bundle, destinationPath);

                UnityEditor.EditorUtility.DisplayProgressBar("Decrypt File: ", bundle.name,
                    (index + 1) / (float)bundles.Count);
            }
            
            foreach (var build in versions.data)
            {
                Copy(GetSourcePath(build.file), GetSavePath(build.file));
            }
            Copy(GetSourcePath(Versions.Filename), GetSavePath(Versions.Filename));
            
            UnityEditor.EditorUtility.ClearProgressBar();
            
            UnityEditor.EditorUtility.DisplayDialog("提示", "解密完成", "明白");
        }

        private void DoDecrypt(ManifestBundle bundle, string filePath)
        {
            if (!bundle.isRaw)
            {
                var uniqueSalt = Encoding.UTF8.GetBytes(bundle.name);
                var data = File.ReadAllBytes(filePath);
                var baseStream = new FileStream(filePath, FileMode.Open);
                using (var cipher = new SeekableAesStream(baseStream, Utility.EncryptKey, uniqueSalt))
                {
                    cipher.Write(data, 0, data.Length);
                }
            }
            else
            {
                Utility.EncryptBinaryFile(filePath);
            }
        }

        private List<ManifestBundle> GetBuildBundles(BuildVersions versions)
        {
            var bundles = new List<ManifestBundle>();
            foreach (var version in versions.data)
            {
                var manifest = Manifest.LoadFromFile(GetSourcePath(version.file));
                bundles.AddRange(manifest.bundles);
            }
            return bundles;
        }
        
        private void Copy(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                Utility.CreateFileDirectory(destinationPath);
                File.Copy(sourcePath, destinationPath, true);
            }
            else
            {
                Debug.LogErrorFormat("File not found: {0}", sourcePath);
            }
        }

        private string GetSourcePath(string fileName)
        {
            return $"{_bundlesPath}/{fileName}";
        }

        private string GetSavePath(string fileName)
        {
            return $"{_destinationPath}/{fileName}";
        }
    }
}