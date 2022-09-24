using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UAsset.Editor
{
    // 1.提供选框，切换显示加载的资源或bundle
    // 2.
    public class RuntimeInfoWindow : EditorWindow
    {
        [SerializeField] private MultiColumnHeaderState _multiColumnHeaderState;
        [SerializeField] private TreeViewState _treeViewState;

        private RuntimeInfoTreeView _treeView;
        
        private List<Loadable> _loadables = new List<Loadable>();
        private readonly Dictionary<int, List<Loadable>> _frameWithLoadables = new Dictionary<int, List<Loadable>>();
        private int _frame;
        
        public static void ShowWindow()
        {
            GetWindow<RuntimeInfoWindow>("运行时信息面板", true);
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                TakeSample();
            }
        }

        private void OnGUI()
        {
            if (_treeView == null)
            {
                _treeViewState = new TreeViewState();
                var headerState = RuntimeInfoTreeView.CreateMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(_multiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(_multiColumnHeaderState, headerState);
                }

                _multiColumnHeaderState = headerState;
                _treeView = new RuntimeInfoTreeView(_treeViewState, headerState);
                _treeView.SetAssets(_loadables);
            }
            
            // var treeRect = GUILayoutUtility.GetLastRect();
            // m_TreeView.OnGUI(new Rect(0, treeRect.yMax, position.width, position.height - treeRect.yMax));
            
            _treeView.OnGUI(new Rect(0, 0, position.width, position.height));
        }

        private void TakeSample()
        {
            _frame = Time.frameCount;

            _loadables = new List<Loadable>();

            foreach (var item in Asset.Cache.Values)
            {
                if (item.isDone)
                {
                    _loadables.Add(item);
                }
            }

            foreach (var item in Bundle.Cache.Values)
            {
                if (item.isDone)
                {
                    _loadables.Add(item);
                }
            }
            
            foreach (var item in RawAsset.Cache.Values)
            {
                if (item.isDone)
                {
                    _loadables.Add(item);
                }
            }
            
            if (Scene.main != null && Scene.main.isDone)
            {
                _loadables.Add(Scene.main);
                _loadables.AddRange(Scene.main.additives);
            }

            _frameWithLoadables[_frame] = _loadables;
            ReloadFrameData();
        }

        private void ReloadFrameData()
        {
            if (_treeView != null)
            {
                _treeView.SetAssets(
                    _frameWithLoadables.TryGetValue(_frame, out var value) ? value : new List<Loadable>());
            }
        }
    }
}