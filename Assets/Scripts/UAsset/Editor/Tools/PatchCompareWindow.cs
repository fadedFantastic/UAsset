using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UAsset.Editor
{
	public class PatchCompareWindow : EditorWindow
	{
		static PatchCompareWindow _thisInstance;
        
        [MenuItem(MenuItems.kBuildPreix + "补丁包比对工具", false, 50)]
        static void ShowWindow()
		{
			if (_thisInstance == null)
			{
				_thisInstance = GetWindow(typeof(PatchCompareWindow), false, "补丁包比对工具", true) as PatchCompareWindow;
				_thisInstance.minSize = new Vector2(800, 600);
			}
			_thisInstance.Show();
		}

		private string _patchManifestPath1 = string.Empty;
		private string _patchManifestPath2 = string.Empty;
		private readonly List<ManifestBundle> _changeList = new List<ManifestBundle>();
		private readonly List<ManifestBundle> _newList = new List<ManifestBundle>();
		private Vector2 _scrollPos1;
		private Vector2 _scrollPos2;
        
        internal bool _changeListFoldout = false;
        internal bool _newFoldout = false;
        
		private void OnGUI()
		{
			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("选择补丁清单1", GUILayout.MaxWidth(150)))
			{
				string resultPath = UnityEditor.EditorUtility.OpenFilePanel("Find", "Bundles/", "json");
				if (string.IsNullOrEmpty(resultPath))
					return;
				_patchManifestPath1 = resultPath;
			}
			EditorGUILayout.LabelField(_patchManifestPath1);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("选择补丁清单2", GUILayout.MaxWidth(150)))
			{
				string resultPath = UnityEditor.EditorUtility.OpenFilePanel("Find", "Bundles/", "json");
				if (string.IsNullOrEmpty(resultPath))
					return;
				_patchManifestPath2 = resultPath;
			}
			EditorGUILayout.LabelField(_patchManifestPath2);
			EditorGUILayout.EndHorizontal();

			if (string.IsNullOrEmpty(_patchManifestPath1) == false && string.IsNullOrEmpty(_patchManifestPath2) == false)
			{
				if (GUILayout.Button("比对差异", GUILayout.MaxWidth(150)))
				{
					ComparePatch(_changeList, _newList);
				}
			}

			EditorGUILayout.Space();
			using (new EditorGUI.DisabledScope(false))
			{
				int totalCount = _changeList.Count;
                _changeListFoldout = EditorGUILayout.Foldout(_changeListFoldout, $"差异列表: ({totalCount})");
                if (_changeListFoldout)
                {
                    EditorGUI.indentLevel++;
                    _scrollPos1 = EditorGUILayout.BeginScrollView(_scrollPos1);
                    {
                        foreach (var bundle in _changeList)
                        {
                            EditorGUILayout.LabelField($"{bundle.nameWithAppendHash} | {bundle.size / 1024}K");
                        }
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;
                }
            }

			EditorGUILayout.Space();
			using (new EditorGUI.DisabledScope(false))
			{
				int totalCount = _newList.Count;
                _newFoldout = EditorGUILayout.Foldout(_newFoldout, $"新增列表(清单2中新增的): ({totalCount}) ");
                if (_newFoldout)
                {
                    EditorGUI.indentLevel++;
                    _scrollPos2 = EditorGUILayout.BeginScrollView(_scrollPos2);
                    {
                        foreach (var bundle in _newList)
                        {
                            EditorGUILayout.LabelField($"{bundle.nameWithAppendHash} | {bundle.size / 1024}K");
                        }
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUI.indentLevel--;   
                }
            }
		}

		private void ComparePatch(List<ManifestBundle> changeList, List<ManifestBundle> newList)
		{
			changeList.Clear();
			newList.Clear();

			// 加载补丁清单1
            var patchManifest1 = Manifest.LoadFromFile(_patchManifestPath1);
            // 加载补丁清单2
            var patchManifest2 = Manifest.LoadFromFile(_patchManifestPath2);

			// 检查文件列表
			foreach (var patchBundle2 in patchManifest2.bundles)
            {
                var patchBundle1 = patchManifest1.GetBundle(patchBundle2.name);
				if (patchBundle1 != null)
				{
					if (patchBundle2.hash != patchBundle1.hash)
					{
						changeList.Add(patchBundle2);
					}
				}
				else
				{
					newList.Add(patchBundle2);
				}
			}

			// 按字母重新排序
			changeList.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.Ordinal));
			newList.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.Ordinal));

			Debug.Log("资源包差异比对完成！");
		}
	}
}