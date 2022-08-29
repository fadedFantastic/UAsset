using System.Collections.Generic;
using System.IO;

namespace UAsset.Editor
{
    public class BuildLua : BuildTaskJob
    {
        public readonly List<ManifestBundle> bundles = new List<ManifestBundle>();
        public const string OUTPUT_PATH = "Library/LuaCache/";
        public const string ROOT_PATH = "Assets/Lua/";
        public const string LUA_EXTENSION = ".lua";
        
        public BuildLua(BuildTask task) : base(task)
        {
        }

        protected override void DoTask()
        {
            //生成 lua
            LuaBuild.GenerateBuildLua(OUTPUT_PATH);
            
            var bundleStartId = _task.bundles.Count;
            int filePathLength = OUTPUT_PATH.Length;
            var files = Directory.GetFiles(OUTPUT_PATH, "*", SearchOption.AllDirectories);
            for (var index = 0; index < files.Length; ++index)
            {
                var filePath = files[index];
                var relFileName = filePath.Substring(filePathLength).Replace("\\", "/");
                var file = new FileInfo(filePath);
                var crc = Utility.ComputeHash(filePath);
                var targetFile = ROOT_PATH + relFileName;
                var assets = new List<string> { targetFile };
                var bundleName = targetFile.ToLower();
                var manifestBundle = new ManifestBundle
                {
                    id = bundleStartId + index,
                    hash = crc,
                    name = bundleName,
                    nameWithAppendHash = $"{bundleName}_{crc}{LUA_EXTENSION}",
                    isRaw = true,
                    assets = assets,
                    size = file.Length,
                    copyToPackage = true
                };
                
                var path = GetBuildPath(manifestBundle.nameWithAppendHash);
                Utility.CreateFileDirectory(path);

                if (!File.Exists(path))
                {
                    file.CopyTo(path);
                }
                bundles.Add(manifestBundle);

                UnityEditor.EditorUtility.DisplayProgressBar("Build Lua", bundleName,
                    (index + 1) / (float)files.Length);
            }
            UnityEditor.EditorUtility.ClearProgressBar();
            
            _task.bundles.AddRange(bundles);
        }
    }
}