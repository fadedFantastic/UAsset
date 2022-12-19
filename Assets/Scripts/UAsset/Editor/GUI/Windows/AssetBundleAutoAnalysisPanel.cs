using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UAsset.Editor
{
    public class AssetBundleAutoAnalysisPanel : EditorWindow
    {
        private static string savePath = BuildRules.kRuleConfigPath;
        private static BuildRules _rules;
        private ReorderableList _list;
        private ReorderableList _variantList;
        private Vector2 _scrollPosition = Vector2.zero;
        private readonly int _assetRootPathLength = "Assets".Length;

        private const float GAP = 5;
        
        public static void ShowWindow()
        {
            GetWindow<AssetBundleAutoAnalysisPanel>("自动分析面板", true);
        }
        
        private void OnGUI()
        {
            if (_rules == null)
            {
                InitConfig();
            }

            if (_list == null)
            {
                InitFilterListDrawer();
            }

            var isAutoAnalysis = false;
            //tool bar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("ViewConfig", EditorStyles.toolbarButton))
                {
                    Selection.objects = new[] {_rules};
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    Save();
                }
            }
            GUILayout.EndHorizontal();

            //context
            GUILayout.BeginVertical();
            {
                //Filter item list
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("AssetBundle ExtName", GUILayout.Width(150));
                    _rules.abExtName = GUILayout.TextField(_rules.abExtName);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Variant Path", GUILayout.Width(100));
                    GUI.enabled = false;
                    GUILayout.TextField(_rules.variantRootPath);
                    GUI.enabled = true;
                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        var path = SelectFolder(Application.dataPath);
                        if (path != null)
                            _rules.variantRootPath = path;
                    }
                    GUILayout.EndHorizontal();

                    _variantList.DoLayoutList();
                    _list.DoLayoutList();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            //set dirty
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_rules);
            }

            if (GUILayout.Button("自动分析"))
            {
                isAutoAnalysis = true;
            }

            if (isAutoAnalysis)
            {
                DoAutoAnalysis();
            }
        }
        
        /// <summary>
        /// 加载规则配置文件
        /// </summary>
        private static void InitConfig()
        {
            _rules = EditorHelper.FindOrCreateAsset<BuildRules>(savePath);
        }
        
        /// <summary>
        /// 初始化规则列表
        /// </summary>
        private void InitFilterListDrawer()
        {
            _variantList = CreateList(_rules.variantRules, "Variant Asset Filter（变体目录第一层目录作为变体种类, 目录名字作为变体名称）");
            _list = CreateList(_rules.rules, "Asset Filter");
        }
        
        /// <summary>
        /// 创建规则列表
        /// </summary>
        /// <param name="data">列表数据</param>
        /// <param name="header">标题</param>
        /// <returns></returns>
        private ReorderableList CreateList(List<BuildRule> data, string header)
        {
            var l = new ReorderableList(data, typeof(BuildRule));
            l.drawElementCallback = OnListElementGUI(data, l);
            l.drawHeaderCallback = OnListHeaderGUI(header);
            l.draggable = true;
            l.elementHeight = 22;
            l.onAddCallback = list => Add(data, list);
            return l;
        }
        
        /// <summary>
        /// 绘制规则列表元素
        /// </summary>
        /// <param name="data">列表数据</param>
        /// <param name="list">列表对象</param>
        /// <returns></returns>
        private ReorderableList.ElementCallbackDelegate OnListElementGUI(List<BuildRule> data, ReorderableList list)
        {
            return (rect, index, isActive, isFocused) =>
            {
                var rule = data[index];
                rect.y++;
                var r = rect;
                
                // 规则是否有效勾选框
                {
                    r.width = 16;
                    r.height = 18;
                    rule.valid = GUI.Toggle(r, rule.valid, GUIContent.none);
                }

                // 显示打包目录
                {
                    r.xMin = r.xMax + GAP;
                    r.xMax = Mathf.Max(rect.xMax - 800, r.xMin + 50);
                    GUI.enabled = false;
                    rule.searchPath = GUI.TextField(r, rule.searchPath);
                    GUI.enabled = true;
                }

                // 选择目录按钮
                {
                    r.xMin = r.xMax + GAP;
                    r.width = 50;
                    if (GUI.Button(r, "Select"))
                    {
                        var topPath = list == _variantList
                            ? Path.Combine(Application.dataPath, _rules.variantRootPath)
                            : Application.dataPath;

                        var path = string.IsNullOrEmpty(rule.searchPath)
                            ? SelectFolder(topPath)
                            : SelectFolder(rule.searchPath, topPath);

                        if (!SetPath(rule, path, list == _variantList))
                        {
                            Debug.LogWarning("目录不符合规则");
                        }
                    }
                }

                // 打包规则
                {
                    r.xMin = r.xMax + GAP;
                    r.width = 150;
                    rule.packRule = (PackRule)EditorGUI.EnumPopup(r, rule.packRule);
                }
                
                
                // 文件过滤规则
                {
                    r.xMin = r.xMax + GAP;
                    r.width = 100;
                    
                    var ruleNames = FileFilterRule.GetFilterRuleNames();
                    var ruleIndex = Array.IndexOf(ruleNames, rule.filterRule);
                    index = EditorGUI.Popup(r, ruleIndex, ruleNames);
                    rule.filterRule = ruleNames[index];
                }

                // 收集器类型
                {
                    r.xMin = r.xMax + GAP;
                    r.width = 160;
                    rule.collectorType = (CollectorType)EditorGUI.EnumPopup(r, rule.collectorType);
                }

                // 手动指定bundle名
                {
                    if (rule.packRule == PackRule.PackExplicit)
                    {
                        r.xMin = r.xMax + GAP;
                        r.width = 80;
                        GUI.Label(r, "BundleName");

                        r.xMin = r.xMax + GAP;
                        r.width = 150;
                        rule.assetBundleName = GUI.TextField(r, rule.assetBundleName);
                    }
                }
                
                // 资源是否进包勾选框
                {
                    r.xMin = r.xMax + GAP;
                    r.width = 50;
                    GUI.Label(r, "Packed");

                    r.xMin = r.xMax + GAP;
                    r.width = 16;
                    r.height = 18;
                    rule.packed = GUI.Toggle(r, rule.packed, GUIContent.none);
                }

                // 资源分组标签
                {
                    r.xMin = r.xMax + GAP;
                    r.width = 30;
                    GUI.Label(r, "Tag");
                    
                    r.xMin = r.xMax + GAP;
                    r.xMax = rect.xMax;
                    rule.tag = GUI.TextField(r, rule.tag);
                }
            };
        }

        /// <summary>
        /// 选中目录
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="topPath"></param>
        /// <returns></returns>
        private string SelectFolder(string rootPath, string topPath = null)
        {
            rootPath = string.IsNullOrEmpty(rootPath)
                ? Application.dataPath
                : rootPath.Replace("\\", "/");

            if (string.IsNullOrEmpty(topPath))
            {
                topPath = rootPath;
            }

            var selectedPath = EditorUtility.OpenFolderPanel("Path", rootPath, "");
            if (string.IsNullOrEmpty(selectedPath)) return null;
            
            if (selectedPath.Equals(topPath))
            {
                return "";
            }

            if (selectedPath.StartsWith(topPath))
            {
                return selectedPath.Substring(topPath.Length - _assetRootPathLength);
            }
            
            ShowNotification(new GUIContent($"不能{rootPath}目录之外!"));
            return null;
        }

        private ReorderableList.HeaderCallbackDelegate OnListHeaderGUI(string header)
        {
            return rect => { EditorGUI.LabelField(rect, header); };
        }

        private void Add(List<BuildRule> data, ReorderableList list)
        {
            var path = SelectFolder(list == _variantList
                ? Path.Combine(Application.dataPath, _rules.variantRootPath)
                : Application.dataPath);
            
            if (!string.IsNullOrEmpty(path))
            {
                var filter = new BuildRule();
                if (SetPath(filter, path, list == _variantList))
                    data.Add(filter);
                else
                {
                    Debug.LogWarning("目录不符合规则");
                }
            }
        }

        private bool SetPath(BuildRule data, string path, bool isVariant)
        {
            if (path == null) return false;
            
            if (isVariant)
            {
                var vs = _rules.GetVariantDirNames();
                foreach (var v in vs)
                {
                    if (path == v)
                    {
                        path = "";
                    }

                    if (path.StartsWith(v))
                    {
                        path = path.Substring(v.Length + 1);
                    }
                }
            }

            data.searchPath = path;
            return true;
        }
        
        public static void AutoAnalysis()
        {
            InitConfig();
            DoAutoAnalysis();
        }

        /// <summary>
        /// 开始自动分析
        /// </summary>
        private static void DoAutoAnalysis()
        {
            _rules.Apply();
            Save();
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        private static void Save()
        {
            EditorUtility.SetDirty(_rules);
            AssetDatabase.SaveAssets();
        }
    }
}