using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UAsset.Editor
{
    public static class BuildScript
    {
        public const string DEFAULT_BUILD_LANGUAGE = "chinese";
        public static Action<BuildTask> postprocessBuildBundles { get; set; }
        public static Action<BuildTask> preprocessBuildBundles { get; set; }

        public static void BuildBundles(BuildTask task)
        {
            preprocessBuildBundles?.Invoke(task);
            task.Run();
            postprocessBuildBundles?.Invoke(task);
        }
        
        /// <summary>
        /// 构建资源
        /// </summary>
        /// <param name="resVersion">资源版本号</param>
        /// <param name="abPath">构建路径</param>
        /// <param name="copyResType">资源包类型</param>
        /// <param name="language">语言</param>
        public static void BuildBundles(int resVersion = -1, string abPath = null, 
            PackageResourceType copyResType = PackageResourceType.Zero, string language = DEFAULT_BUILD_LANGUAGE)
        {
            BuildBundles(new BuildTask(resVersion, abPath, copyResType, language));
        }

        /// <summary>
        /// 拷贝打包资源到StreamingAssets目录
        /// </summary>
        /// <param name="type">安装包资源类型</param>
        /// <param name="language">安装包默认语言</param>
        public static void CopyToStreamingAssets(PackageResourceType type = PackageResourceType.Zero, 
            string language = DEFAULT_BUILD_LANGUAGE)
        {
            var destinationDir = Settings.BuildPlayerDataPath;
            if (Directory.Exists(destinationDir))
            {
                Directory.Delete(destinationDir, true);
            }
            Directory.CreateDirectory(destinationDir);

            var settings = Settings.GetDefaultSettings();
            var versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));

            if (type == PackageResourceType.Full)
            {
                var bundles = settings.GetBundlesInBuild(versions);
                for (var index = 0; index < bundles.Count; index++)
                {
                    var bundle = bundles[index];
                    if ((bundle.IsVariant && bundle.variant != language) || !bundle.copyToPackage || bundle.IsWithTag)
                    {
                        bundles.RemoveAt(index);
                        --index;
                    }
                    else
                    {
                        EditorUtility.Copy(bundle.nameWithAppendHash, destinationDir);
                    }

                    UnityEditor.EditorUtility.DisplayProgressBar("Copy Bundle To StreamingAssets", bundle.nameWithAppendHash,
                        (index + 1) / (float)bundles.Count);
                }
                
                versions.streamingAssets = bundles.ConvertAll(o => o.nameWithAppendHash);
            }

            foreach (var build in versions.data)
            {
                EditorUtility.Copy(build.file, destinationDir);
            }
            
            versions.encryptionEnabled = settings.encryptionEnabled;
            versions.offlineMode = false;
            File.WriteAllText($"{destinationDir}/{Versions.Filename}", JsonUtility.ToJson(versions, true));
            
            UnityEditor.EditorUtility.ClearProgressBar();
        }

        public static void ClearBuildFromSelection()
        {
            var filtered = Selection.GetFiltered<Object>(SelectionMode.DeepAssets);
            var assetPaths = new List<string>();
            foreach (var o in filtered)
            {
                var assetPath = AssetDatabase.GetAssetPath(o);
                if (string.IsNullOrEmpty(assetPath)) continue;

                assetPaths.Add(assetPath);
            }

            var bundles = new List<string>();
            var versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));
            foreach (var version in versions.data)
            {
                var manifest = Manifest.LoadFromFile(Settings.GetBuildPath(version.file));
                foreach (var assetPath in assetPaths)
                {
                    var bundle = manifest.GetBundle(assetPath);
                    if (bundle != null) bundles.Add(bundle.nameWithAppendHash);
                }
            }

            foreach (var bundle in bundles)
            {
                var file = Settings.GetBuildPath(bundle);
                if (!File.Exists(file)) continue;

                File.Delete(file);
                Debug.LogFormat("Delete:{0}", file);
            }
        }

        /// <summary>
        /// 清理历史打包数据，只留下当前版本的资源和原始的bundle(用于增量打包)
        /// </summary>
        public static void ClearHistory()
        {
            var usedFiles = new List<string>
            {
                Settings.GetPlatformName(),
                Settings.GetPlatformName() + ".manifest",
                Versions.Filename
            };

            var versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));
            foreach (var version in versions.data)
            {
                usedFiles.Add(version.file);
                usedFiles.Add(version.name + ".bin");
                var manifest = Manifest.LoadFromFile(Settings.GetBuildPath(version.file));
                foreach (var bundle in manifest.bundles)
                {
                    usedFiles.Add(bundle.name);
                    usedFiles.Add($"{bundle.name}.manifest");
                    usedFiles.Add(bundle.nameWithAppendHash);
                }
            }

            var files = Directory.GetFiles(Settings.PlatformBuildPath);
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (usedFiles.Contains(name)) continue;

                File.Delete(file);
                Debug.LogFormat("Delete {0}", file);
            }
        }

        /// <summary>
        /// 删掉打包目录
        /// </summary>
        public static void ClearBuild(bool showTips = true)
        {
            if (showTips)
            {
                if (!UnityEditor.EditorUtility.DisplayDialog("提示", "清理构建数据将无法正常增量打包，确认清理？", "确定")) return;   
            }

            var buildPath = Settings.PlatformBuildPath;
            Directory.Delete(buildPath, true);
        }
    }
}