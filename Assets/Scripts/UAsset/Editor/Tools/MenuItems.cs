using System.IO;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    public static class MenuItems
    {
        internal const string kResourceMenuPrefix = "Tools/ResourceManagerV2/";
        internal const string kBuildPreix = "Tools/Build/";

        [MenuItem(kResourceMenuPrefix + "Editor Mode", priority = -11)]
        public static void EditorModeMenu()
        {
            var setting = Settings.GetDefaultSettings();
            setting.scriptPlayMode = ScriptPlayMode.Simulation;
            UnityEditor.EditorUtility.SetDirty(setting);
        }
        [MenuItem(kResourceMenuPrefix + "Editor Mode", validate = true)]
        public static bool EditorModeMenu_Validate()
        {
            var setting = Settings.GetDefaultSettings();
            Menu.SetChecked(kResourceMenuPrefix + "Editor Mode", setting.scriptPlayMode == ScriptPlayMode.Simulation);
            return true;
        }
        
        [MenuItem(kResourceMenuPrefix + "Package Mode", priority = -11)]
        public static void PackageModMenu()
        {
            var setting = Settings.GetDefaultSettings();
            setting.scriptPlayMode = ScriptPlayMode.Preload;
            UnityEditor.EditorUtility.SetDirty(setting);
        }
        [MenuItem(kResourceMenuPrefix + "Package Mode", validate = true)]
        public static bool PackageModeMenu_Validate()
        {
            var setting = Settings.GetDefaultSettings();
            Menu.SetChecked(kResourceMenuPrefix + "Package Mode", setting.scriptPlayMode == ScriptPlayMode.Preload);
            return true;
        }
        
        [MenuItem(kResourceMenuPrefix + "Updatable Mode", priority = -10)]
        public static void UpdatableModeMenu()
        {
            var setting = Settings.GetDefaultSettings();
            setting.scriptPlayMode = ScriptPlayMode.Increment;
            UnityEditor.EditorUtility.SetDirty(setting);
        }
        [MenuItem(kResourceMenuPrefix + "Updatable Mode", validate = true)]
        public static bool UpdatableModeMenu_Validate()
        {
            var setting = Settings.GetDefaultSettings();
            Menu.SetChecked(kResourceMenuPrefix + "Updatable Mode", setting.scriptPlayMode == ScriptPlayMode.Increment);
            return true;
        }

        [MenuItem(kBuildPreix + "Copy Build to StreamingAssets", false, 50)]
        public static void CopyBuildToStreamingAssets()
        {
            BuildScript.CopyToStreamingAssets(PackageResourceType.Full);
        }

        [MenuItem(kBuildPreix + "Clear Build", false, 800)]
        public static void ClearBuild()
        {
            BuildScript.ClearBuild();
        }

        [MenuItem(kBuildPreix + "Clear Build from selection", false, 800)]
        public static void ClearBuildFromSelection()
        {
            BuildScript.ClearBuildFromSelection();
        }

        [MenuItem(kBuildPreix + "Clear History(打包目录只留下当前版本的资源)", false, 800)]
        public static void ClearHistory()
        {
            BuildScript.ClearHistory();
        }
        
        [MenuItem(kBuildPreix + "Clear Download(清理下载目录)", false, 800)]
        public static void ClearDownload()
        {
            Versions.ClearDownload();
        }
        
        [MenuItem(kBuildPreix + "Select xasset Settings", false, 850)]
        public static void SelectSettings()
        {
            var settings = Settings.GetDefaultSettings();
            EditorGUIUtility.PingObject(settings);
            Selection.activeObject = settings;
        }
    }
}