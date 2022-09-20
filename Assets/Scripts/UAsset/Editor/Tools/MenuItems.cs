using System.IO;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    public static class MenuItems
    {
        internal const string kUAssetToolMenu = "UAsset/";

        #region Script Play Mode

        [MenuItem(kUAssetToolMenu + "Script Play Mode/Editor Mode", priority = -11)]
        public static void EditorModeMenu()
        {
            SetScriptPlayMode(ScriptPlayMode.Simulation);
        }
        [MenuItem(kUAssetToolMenu + "Script Play Mode/Editor Mode", validate = true)]
        public static bool EditorModeMenu_Validate()
        {
            return ValidateScriptPlayMode("Script Play Mode/Editor Mode", ScriptPlayMode.Simulation);
        }
        
        [MenuItem(kUAssetToolMenu + "Script Play Mode/Package Mode", priority = -11)]
        public static void PackageModMenu()
        {
            SetScriptPlayMode(ScriptPlayMode.Preload);
        }
        [MenuItem(kUAssetToolMenu + "Script Play Mode/Package Mode", validate = true)]
        public static bool PackageModeMenu_Validate()
        {
            return ValidateScriptPlayMode("Script Play Mode/Package Mode", ScriptPlayMode.Preload);
        }
        
        [MenuItem(kUAssetToolMenu + "Script Play Mode/Updatable Mode", priority = -11)]
        public static void UpdatableModeMenu()
        {
            SetScriptPlayMode(ScriptPlayMode.Increment);
        }
        [MenuItem(kUAssetToolMenu + "Script Play Mode/Updatable Mode", validate = true)]
        public static bool UpdatableModeMenu_Validate()
        {
            return ValidateScriptPlayMode("Script Play Mode/Updatable Mode", ScriptPlayMode.Increment);
        }
        
        private static void SetScriptPlayMode(ScriptPlayMode mode)
        {
            var setting = Settings.GetDefaultSettings();
            setting.scriptPlayMode = mode;
            UnityEditor.EditorUtility.SetDirty(setting);
        }

        private static bool ValidateScriptPlayMode(string menuName, ScriptPlayMode mode)
        {
            var setting = Settings.GetDefaultSettings();
            Menu.SetChecked(kUAssetToolMenu + menuName, setting.scriptPlayMode == mode);
            return true;
        }
        
        #endregion

        [MenuItem(kUAssetToolMenu + "Clear Build", false, 800)]
        public static void ClearBuild()
        {
            BuildScript.ClearBuild();
        }

        [MenuItem(kUAssetToolMenu + "Clear Build from selection", false, 800)]
        public static void ClearBuildFromSelection()
        {
            BuildScript.ClearBuildFromSelection();
        }

        [MenuItem(kUAssetToolMenu + "Clear History(打包目录只留下当前版本的资源)", false, 800)]
        public static void ClearHistory()
        {
            BuildScript.ClearHistory();
        }
        
        [MenuItem(kUAssetToolMenu + "Clear Download(清理下载目录)", false, 800)]
        public static void ClearDownload()
        {
            Versions.ClearDownload();
        }
        
        [MenuItem(kUAssetToolMenu + "Copy Build to StreamingAssets", false, 800)]
        public static void CopyBuildToStreamingAssets()
        {
            BuildScript.CopyToStreamingAssets(PackageResourceType.Full);
        }
        
        [MenuItem(kUAssetToolMenu + "Select UAsset Settings", false, 850)]
        public static void SelectSettings()
        {
            var settings = Settings.GetDefaultSettings();
            EditorGUIUtility.PingObject(settings);
            Selection.activeObject = settings;
        }
        
        // TODO: 临时的，写个面板
        [MenuItem(kUAssetToolMenu + "Build AssetBundle")]
        public static void BuildAssetBundle()
        {
            AssetBundleAutoAnalysisPanel.AutoAnalysis();
            BuildScript.BuildBundles();
        }
    }
}