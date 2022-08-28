using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    public class LuaBuild
    {
#if UNITY_IPHONE && UNITY_EDITOR_OSX
        private  static readonly string _luajit = Application.dataPath + "/../Lua/Dist/mac/ios64";
#elif UNITY_EDITOR_OSX && UNITY_ANDROID
        private  static readonly string _luajit = Application.dataPath + "/../Lua/Dist/mac/android32";
#else
        private static readonly string _luajit = Application.dataPath + "/../Lua/Dist/windows/android32";
#endif

        private static bool IS_OPEN_LUAJIT = false;
        
        public static void GenerateBuildLua(string path)
        {
            DeleteBuildLua(path);
            Utility.CreateDirectory(path);

            StringBuilder strErr = new StringBuilder();
            bool hasErr = false;
            CopyLuaBytesFilesJit(ref hasErr, Application.dataPath + "/../Lua/", path, IS_OPEN_LUAJIT,
                strErr);

            if (hasErr)
            {
                Exception ex = new Exception(strErr.ToString());
                throw ex;
            }

            AssetDatabase.Refresh();
        }
        
        public static void DeleteBuildLua(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            var meta = path + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }
            AssetDatabase.Refresh();
        }

        private static void CopyLuaBytesFilesJit(ref bool hasErr, string sourceDir, string destDir,
            bool luajitcode = false, StringBuilder strErr = null)
        {
            if (!Directory.Exists(sourceDir))
            {
                Debug.LogError($"输入路径不存在: {sourceDir}");
                return;
            }

            int len = destDir.Length;
            if (destDir[len - 1] != '/' && destDir[len - 1] != '\\')
            {
                destDir += "/";
            }

            len = sourceDir.Length;
            if (sourceDir[len - 1] != '/' && sourceDir[len - 1] != '\\')
            {
                destDir += "/";
            }

            len = sourceDir.Length;

            string[] files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string srcPath = files[i];
                if (srcPath.Contains(".svn")) continue;
                if (srcPath.Contains(".git")) continue;
                if (srcPath.Contains(".idea")) continue;
                if (srcPath.Contains(".meta")) continue;
                if (srcPath.Contains(".vs")) continue;
                if (srcPath.Contains(".md")) continue;
                string str = files[i].Remove(0, len);
                // str = str.Replace(".bytes", "");
                string dest = destDir + str + ".bytes";
                
                Utility.CreateFileDirectory(dest);
 
                //TODO: lua目录最好不要乱放文件
                var ext = Path.GetExtension(str);
                if(ext != ".lua" && ext !=".pb") continue;
                
                if (luajitcode && ext == ".lua")
                {
                    LuaJit(srcPath, dest, ref strErr);
                }
                else
                {
                    File.Copy(srcPath, dest);
                }
            }
        }

        private static bool LuaJit(string sourceDir, string destPath, ref StringBuilder strErr)
        {
            using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
            {
                proc.StartInfo.FileName = string.Format("{0}/luajit", _luajit);
                proc.StartInfo.WorkingDirectory = _luajit;
                proc.StartInfo.Arguments = string.Format("-b {0} {1}", sourceDir, destPath);
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardInput = true;
                proc.EnableRaisingEvents = true;

                StringBuilder errorSb = new StringBuilder();
                proc.ErrorDataReceived += (sender, args) => errorSb.Append(args.Data);

                proc.Start();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                if (errorSb.Length > 0)
                {
                    if (null != strErr)
                    {
                        strErr.Append(errorSb.ToString() + "\n");
                    }

                    Debug.LogError(errorSb.ToString());
                    return false;
                }
            }

            return true;
        }
    }
}