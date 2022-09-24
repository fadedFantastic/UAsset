using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UAsset.Editor
{
    /// <summary>
    /// 拷贝资源到版本目录
    /// </summary>
    public class CopyResToVersionPath : BuildTaskJob
    {
        public CopyResToVersionPath(BuildTask task) : base(task)
        {
        }
        
        protected override void DoTask()
        {
            var versions = BuildVersions.Load(Settings.GetBuildPath(Versions.Filename));
            var destinationDir = Settings.GetBuildVersionDir(versions);
            if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
            Directory.CreateDirectory(destinationDir);
            
            Debug.Log("version path :::   " + destinationDir);

            var settings = Settings.GetDefaultSettings();
            var bundles = settings.GetBundlesInBuild(versions);
            for (var index = 0; index < bundles.Count; index++)
            {
                var bundle = bundles[index];
                EditorHelper.Copy(bundle.nameWithAppendHash, destinationDir);
                
                UnityEditor.EditorUtility.DisplayProgressBar("Copy Bundle To Version Platform Path", bundle.nameWithAppendHash,
                    (index + 1) / (float)bundles.Count);
            }

            foreach (var build in versions.data)
            {
                EditorHelper.Copy(build.file, destinationDir);
            }

            EditorHelper.Copy(Versions.Filename, destinationDir);
            
            UnityEditor.EditorUtility.ClearProgressBar();
        }
    }
}