using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    public class BundleBuildWindow : EditorWindow
    {
        /// <summary>
        /// 构建参数
        /// </summary>
        private BundleBuildParameters _bundleBuildParameters;

        public static void ShowWindow()
        {
            var window = GetWindow<BundleBuildWindow>("资源包构建窗口", true, typeof(AssetBundleAutoAnalysisPanel));
            window._bundleBuildParameters = EditorHelper.LoadSettingData<BundleBuildParameters>();
            
            GetWindow<AssetBundleAutoAnalysisPanel>("自动分析面板", false, typeof(BundleBuildWindow));
            
            window.Focus();
        }

        private void OnGUI()
        {
            DrawBundleBuildConfigView();
        }
        
        private void DrawBundleBuildConfigView()
        {
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("游戏版本号：" + Application.version, GUILayout.Width(200));
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("资源清单版本号：", GUILayout.Width(100));
                _bundleBuildParameters.manifestVersion =
                    EditorGUILayout.IntField(_bundleBuildParameters.manifestVersion);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("选择资源包构建平台：");
            // TODO: 平台
            //_bundleBuildParameters.targetPlatforms = EditorGUILayout.Popup()

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("选择资源包构建设置：");
            _bundleBuildParameters.buildOptions =
                (BuildAssetBundleOptions) EditorGUILayout.EnumFlagsField(_bundleBuildParameters.buildOptions);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("资源包构建输出根目录：", GUILayout.Width(150));
                _bundleBuildParameters.abPath = GUILayout.TextField(_bundleBuildParameters.abPath);
                if (GUILayout.Button("选择目录", GUILayout.Width(100)))
                {
                    var folder = EditorUtility.OpenFolderPanel("选择资源包构建输出根目录",
                        _bundleBuildParameters.abPath, "");
                    if (folder != string.Empty)
                    {
                        _bundleBuildParameters.abPath = folder;
                    }
                }
            }
            
            EditorGUILayout.Space();
            
            using (var toggle = new EditorGUILayout.ToggleGroupScope("开启资源加密",
                       _bundleBuildParameters.encryptionEnable))
            {
                _bundleBuildParameters.encryptionEnable = toggle.enabled;
            }

            EditorGUILayout.Space();

            using (var toggle = new EditorGUILayout.ToggleGroupScope("在构建完成后将其复制到StreamingAssets目录下",
                       _bundleBuildParameters.copyToStreamingAssets))
            {
                _bundleBuildParameters.copyToStreamingAssets = toggle.enabled;
            }

            EditorGUILayout.Space();

            using (var toggle = new EditorGUILayout.ToggleGroupScope("开启自动分析依赖", 
                       _bundleBuildParameters.runRedundancyAnalyze))
            {
                _bundleBuildParameters.runRedundancyAnalyze = toggle.enabled;
            }
            
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("构建资源包"))
                {
                    if (_bundleBuildParameters.runRedundancyAnalyze)
                    {
                        AssetBundleAutoAnalysisPanel.AutoAnalysis();
                    }
                    
                    BuildScript.BuildBundles(_bundleBuildParameters);

                    _bundleBuildParameters.manifestVersion++;
                    EditorUtility.SetDirty(_bundleBuildParameters);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    return;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_bundleBuildParameters);
                AssetDatabase.SaveAssets();
            }
        }
    }
}