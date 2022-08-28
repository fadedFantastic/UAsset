using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    public class AssetBundleAutoAnalysisPanel : EditorWindow
    {
        [MenuItem("Tools/ResourceManagerV2/自动分析面板")]
        static void Open()
        {
            GetWindow<AssetBundleAutoAnalysisPanel>("自动分析面板", true);
        }

        static T LoadAssetAtPath<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static string savePath = BuildRules.ruleConfigPath;
        private static BuildRules _rules;
        private ReorderableList _list;
        private ReorderableList _variantList;
        private Vector2 _scrollPosition = Vector2.zero;

        AssetBundleAutoAnalysisPanel()
        {
        }

        ReorderableList.ElementCallbackDelegate OnListElementGUI(List<BuildRule> data, ReorderableList list)
        {
            return (Rect rect, int index, bool isactive, bool isfocused) =>
            {
                const float GAP = 5;

                BuildRule rule = data[index];
                rect.y++;

                Rect r = rect;
                r.width = 16;
                r.height = 18;
                rule.valid = GUI.Toggle(r, rule.valid, GUIContent.none);


                r.xMin = r.xMax + GAP;
                r.xMax = Mathf.Max(rect.xMax - 800, r.xMin + 50);
                GUI.enabled = false;
                rule.searchPath = GUI.TextField(r, rule.searchPath);
                GUI.enabled = true;

                r.xMin = r.xMax + GAP;
                r.width = 50;
                if (GUI.Button(r, "Select"))
                {
                    var topPath = list == _variantList
                        ? Path.Combine(Application.dataPath, _rules.variantPath)
                        : Application.dataPath;
                    string path = string.IsNullOrEmpty(rule.searchPath)
                        ? SelectFolder(topPath)
                        : SelectFolder(rule.searchPath, topPath);
                    if (!SetPath(rule, path, list == _variantList))
                    {
                        Debug.LogWarning("目录不符合规则");
                    }
                }

                r.xMin = r.xMax + GAP;
                r.width = 150;
                rule.loadType = (LoadType) EditorGUI.EnumPopup(r, rule.loadType);

                r.xMin = r.xMax + GAP;
                r.width = 50;
                GUI.Label(r, "Packed");

                r.xMin = r.xMax + GAP;
                r.width = 16;
                r.height = 18;
                rule.packed = GUI.Toggle(r, rule.packed, GUIContent.none);

                r.xMin = r.xMax + GAP;
                r.width = 100;
                rule.nameBy = (NameBy) EditorGUI.EnumPopup(r, rule.nameBy);

                if (rule.nameBy == NameBy.Explicit)
                {
                    r.xMin = r.xMax + GAP;
                    r.width = 80;
                    GUI.Label(r, "BundleName");

                    r.xMin = r.xMax + GAP;
                    r.width = 150;
                    rule.assetBundleName = GUI.TextField(r, rule.assetBundleName);
                }

                r.xMin = r.xMax + GAP;
                r.width = 30;
                GUI.Label(r, "Tag");
                
                r.xMin = r.xMax + GAP;
                r.width = 80;
                rule.tag = GUI.TextField(r, rule.tag);
                
                r.xMin = r.xMax + GAP;
                r.width = 50;
                GUI.Label(r, "Pattern");

                r.xMin = r.xMax + GAP;
                r.xMax = rect.xMax;
                rule.searchPattern = GUI.TextField(r, rule.searchPattern);
            };
        }

        string SelectFolder(string rootPath, string topPath = null)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Application.dataPath;
            }
            else
            {
                rootPath = rootPath.Replace("\\", "/");
            }

            if (string.IsNullOrEmpty(topPath))
            {
                topPath = rootPath;
            }

            string selectedPath = UnityEditor.EditorUtility.OpenFolderPanel("Path", rootPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.Equals(topPath))
                {
                    return "";
                }

                if (selectedPath.StartsWith(topPath))
                {
                    return selectedPath.Substring(topPath.Length + 1);
                }
                else
                {
                    ShowNotification(new GUIContent($"不能{rootPath}目录之外!"));
                }
            }

            return null;
        }

        ReorderableList.HeaderCallbackDelegate OnListHeaderGUI(string header)
        {
            return (rect) => { EditorGUI.LabelField(rect, header); };
        }

        static void InitConfig()
        {
            _rules = LoadAssetAtPath<BuildRules>(savePath);
            if (_rules == null)
            {
                _rules = CreateInstance<BuildRules>();
            }
        }

        void InitFilterListDrawer()
        {
            _list = CreateList(_rules.rules, "Asset Filter");
            _variantList = CreateList(_rules.variantRules, "Variant Asset Filter（变体目录第一层目录作为变体种类, 目录名字作为变体名称）");
        }

        ReorderableList CreateList(List<BuildRule> data, string header)
        {
            var l = new ReorderableList(data, typeof(BuildRule));
            l.drawElementCallback = OnListElementGUI(data, l);
            l.drawHeaderCallback = OnListHeaderGUI(header);
            l.draggable = true;
            l.elementHeight = 22;
            l.onAddCallback = (list) => Add(data, list);
            return l;
        }

        void Add(List<BuildRule> data, ReorderableList list)
        {
            string path = SelectFolder(list == _variantList
                ? Path.Combine(Application.dataPath, _rules.variantPath)
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

        bool SetPath(BuildRule data, string path, bool isVariant)
        {
            if (path == null) return false;
            if (isVariant)
            {
                var vs = _rules.GetVariants();
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

        void OnGUI()
        {
            if (_rules == null)
            {
                InitConfig();
            }

            if (_list == null)
            {
                InitFilterListDrawer();
            }


            bool isAutoAnalysis = false;
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
                    GUILayout.TextField(_rules.variantPath);
                    GUI.enabled = true;
                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        var path = SelectFolder(Application.dataPath);
                        if (path != null)
                            _rules.variantPath = path;
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
                UnityEditor.EditorUtility.SetDirty(_rules);

            if (GUILayout.Button("自动分析"))
            {
                isAutoAnalysis = true;
            }

            if (isAutoAnalysis)
            {
                DoAutoAnalysis();
            }
        }

        [MenuItem("Tools/Build/Auto Analysis", priority = 10)]
        public static void AutoAnalysis()
        {
            InitConfig();
            DoAutoAnalysis();
        }

        private static void DoAutoAnalysis()
        {
            // LuaBuild.GenerateBuildLua();
            _rules.Apply();
            
            // var assets = _rules.ruleAssets;
            // var bundles = _rules.ruleBundles;
            //
            // //FIXME: 这个地方考虑手动指定打包方式，如果后面不需要可以不要load xml 直接重新生成并覆盖xml
            // var source = new ResourceCollection();
            // source.Load();
            //
            //
            // Debug.Log($"自动分析结构 bundles： {bundles.Length} assets: {assets.Length}");
            //
            // var dest = new ResourceCollection();
            //
            // for (int i = 0; i < bundles.Length; i++)
            // {
            //     var bundle = bundles[i];
            //     var name = FixName(bundle.name);
            //     var res = source.GetResource(name, bundle.variant);
            //     if (res != null)
            //     {
            //         dest.AddResource(name, bundle.variant, res.FileSystem, res.LoadType, res.Packed);
            //     }
            //     else
            //     {
            //         dest.AddResource(name, bundle.variant, bundle.fileSystem, bundle.loadType, bundle.packed);
            //     }
            //     
            // }
            //
            // for (int i = 0; i < assets.Length; i++)
            // {
            //     var asset = assets[i];
            //     var name = FixName(asset.bundle);
            //     dest.AssignAsset(asset.guid, name, asset.resourceVariant);
            // }
            //
            // // try
            // // {
            // //     AnalysisLua(dest);
            // // }
            // // catch (Exception e)
            // // {
            // //     Debug.LogError(e);
            // //     EditorUtility.ClearProgressBar();
            // // }
            // dest.Save();
        }

        static string FixName(string n)
        {
            return n + "_" + _rules.abExtName;
        }

        // private static void AnalysisLua(ResourceCollection dest)
        // {
        //     var LuaPath = "Assets" + LuaBuild.LUA_OUTPUT_PATH.Replace(Application.dataPath,"");
        //     
        //     var files = Directory.GetFiles(LuaPath, "*.bytes", System.IO.SearchOption.AllDirectories);
        //     
        //     for (int i = 0; i < files.Length; i++)
        //     {
        //         var file = files[i];
        //         file = Utility.Path.GetRegularPath(file);
        //         
        //         if (EditorUtility.DisplayCancelableProgressBar(string.Format("处理Lua文件{0}/{1}", i, files.Length), file,
        //             i / (float) files.Length)) break;
        //         
        //         var rootPath = Path.GetDirectoryName(file);
        //         rootPath = Utility.Path.GetRegularPath(rootPath);
        //
        //         var fileSystemName = LuaPath + "/" + rootPath.Substring(7).Replace('/', '_');
        //         var resourceName = fileSystemName +"_"+Path.GetFileNameWithoutExtension(file);
        //         Resource resource = dest.GetResource(resourceName,null);
        //         if (resource == null)
        //         {
        //             dest.AddResource(resourceName, null, null, LoadType.LoadFromBinary, true);
        //             resource = dest.GetResource(resourceName, null);
        //             resource.Packed = true;
        //         }
        //         if (resource != null)
        //         {
        //             resource.FileSystem = fileSystemName;
        //             resource.LoadType = LoadType.LoadFromBinary;
        //             var asset = AssetDatabase.AssetPathToGUID(file);
        //             dest.AssignAsset(asset, resourceName, null);
        //         }
        //     }
        //     EditorUtility.ClearProgressBar();
        // }

        void Save()
        {
            if (LoadAssetAtPath<BuildRules>(savePath) == null)
            {
                AssetDatabase.CreateAsset(_rules, savePath);
            }
            else
            {
                UnityEditor.EditorUtility.SetDirty(_rules);
            }
            AssetDatabase.SaveAssets();
        }
    }
}