using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UAsset.Editor
{
    public class BuildTask
    {
        public readonly List<ManifestBundle> bundles = new List<ManifestBundle>();
        public readonly List<string> changes = new List<string>();
        public readonly List<BuildTaskJob> jobs = new List<BuildTaskJob>();
        public readonly string outputPath;
        public readonly Stopwatch stopwatch = new Stopwatch();
        public int buildVersion;
        public bool forceRebuild;

        public string buildVariant { get; }
        public BuildRules buildRules { get; }
        public PackageResourceType packageResourceType { get; }

        public BuildTask(int version, string abPath, PackageResourceType copyResType, string variant) : this("Manifest")
        {
            buildVersion = version;
            packageResourceType = copyResType;
            buildVariant = variant;
            buildRules = GetBuildRules();
            
            Utility.BuildPath = abPath ?? Utility.BuildPath;
            outputPath = Settings.PlatformBuildPath;

            jobs.Add(new VerifyBuild(this));
            jobs.Add(new BuildBundles(this));
            jobs.Add(new BuildLua(this));
            jobs.Add(new EncryptFile(this));
            jobs.Add(new CreateManifest(this));
            jobs.Add(new CopyResToVersionPath(this));
            jobs.Add(new CopyResToPackage(this));
        }

        public BuildTask(string build)
        {
            Settings.GetDefaultSettings().Initialize();
            name = build;
        }

        public string name { get; }

        public void Run()
        {
            stopwatch.Start();
            foreach (var job in jobs)
            {
                try
                {
                    job.Run();
                }
                catch (Exception e)
                {
                    job.error = e.Message;
                    Debug.LogException(e);
                }

                if (string.IsNullOrEmpty(job.error)) continue;
                break;
            }

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds / 1000f;
            Debug.LogFormat("Run BuildTask for {0} with {1}s", name, elapsed);
        }


        public string GetBuildPath(string filename)
        {
            return $"{outputPath}/{filename}";
        }

        public void SaveManifest(Manifest manifest)
        {
            var timestamp = DateTime.Now.ToFileTime();
            manifest.name = name.ToLower();
            var filename = $"{manifest.name}.json";
            File.WriteAllText(GetBuildPath(filename), JsonUtility.ToJson(manifest, true));
            var path = GetBuildPath(filename);
            var hash = Utility.ComputeHash(path);
            var file = $"{manifest.name}_v{manifest.version}_{hash}.json";
            File.Move(GetBuildPath(filename), GetBuildPath(file));
            changes.Add(file);
            // save version
            SaveVersion(file, timestamp, hash);
        }

        private void SaveVersion(string file, long timestamp, string hash)
        {
            var info = new FileInfo(GetBuildPath(file));
            var buildVersions = BuildVersions.Load(GetBuildPath(Versions.Filename));
            buildVersions.Set(name, file, info.Length, timestamp, hash, Application.version, buildVersion);
            buildVersions.encryptionEnabled = Settings.EncryptionEnabled;
            buildVersions.buildinVariant = buildVariant;
            buildVersions.variantTypes = buildRules.variantDirNames;
            buildVersions.variantVersion = Enumerable.Repeat(0, buildVersions.variantTypes.Length).ToList();;
            
            File.WriteAllText(GetBuildPath(Versions.Filename), JsonUtility.ToJson(buildVersions, true));
        }
        
        private BuildRules GetBuildRules ()
        {
            return EditorUtility.FindOrCreateAsset<BuildRules>(BuildRules.ruleConfigPath);
        }
    }
}