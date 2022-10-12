using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UAsset.Editor
{
    public enum RuntimeInfoWindowMode
    {
        AssetView,
        BundleView
    };
    
    public class RuntimeInfoWindow : EditorWindow
    {
        private const int k_ToolbarHeight = 40;
        [SerializeField] private MultiColumnHeaderState _assetMultiColumnHeaderState;
        [SerializeField] private MultiColumnHeaderState _bundleMultiColumnHeaderState;
        [SerializeField] private TreeViewState _assetTreeViewState;
        [SerializeField] private TreeViewState _bundleTreeViewState;

        private RuntimeAssetTreeView _assetTreeView;
        private RuntimeBundleTreeView _bundleTreeView;
        
        private List<Loadable> _assets = new List<Loadable>();
        private List<Bundle> _bundles = new List<Bundle>();
        private readonly Dictionary<int, List<Loadable>> _frameWithAssets = new Dictionary<int, List<Loadable>>();
        private readonly Dictionary<int, List<Bundle>> _frameWithBundles = new Dictionary<int, List<Bundle>>();
        
        private RuntimeInfoWindowMode _mode = RuntimeInfoWindowMode.AssetView;
        private VerticalSplitter _verticalSplitter;
        
        private bool _recording = true;
        private int _currentFrame;
        private int _frame;

        private SearchField _searchField;

        public static void ShowWindow()
        {
            var window = GetWindow<RuntimeInfoWindow>("运行时信息面板", true);
            window.minSize = new Vector2(1000, 600);
        }

        private void Update()
        {
            if (_recording && Application.isPlaying)
            {
                TakeSample();
            }
        }

        private void OnGUI()
        {
            if (_verticalSplitter == null)
            {
                _verticalSplitter = new VerticalSplitter();
            }
            
            if (_assetTreeView == null)
            {
                _assetTreeViewState = new TreeViewState();
                var headerState = RuntimeAssetTreeView.CreateMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(_assetMultiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(_assetMultiColumnHeaderState, headerState);
                }

                _assetMultiColumnHeaderState = headerState;
                _assetTreeView = new RuntimeAssetTreeView(_assetTreeViewState, headerState, this);
                _assetTreeView.SetAssets(_assets);
            }

            if (_bundleTreeView == null)
            {
                _bundleTreeViewState = new TreeViewState();
                var headerState = RuntimeBundleTreeView.CreateMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(_bundleMultiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(_bundleMultiColumnHeaderState, headerState);
                }
                
                _bundleTreeView = new RuntimeBundleTreeView(_bundleTreeViewState, headerState);
                _bundleTreeView.SetBundles(_bundles);
            }

            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.downOrUpArrowKeyPressed += _assetTreeView.SetFocusAndEnsureSelectedItem;
            }

            var rect = new Rect(0, 0, position.width, position.height);
            
            DrawToolbar();
            DrawTreeView(rect);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _mode = (RuntimeInfoWindowMode)EditorGUILayout.EnumPopup(_mode, EditorStyles.toolbarDropDown, GUILayout.Width(100));
                _assetTreeView.searchString = _searchField.OnToolbarGUI(_assetTreeView.searchString);
            }

            // 帧
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _recording = GUILayout.Toggle(_recording, "Record", EditorStyles.toolbarButton, GUILayout.Width(60));
                
                GUILayout.Label("Frame:", GUILayout.Width(80));
                if (GUILayout.Button("<<", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    _frame = Mathf.Max(0, _frame - 1);
                    ReloadFrameData();
                    _recording = false;
                }
                
                if (GUILayout.Button(">>", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    _frame = Mathf.Min(_frame + 1, Time.frameCount);
                    ReloadFrameData();
                    _recording = false;
                }
                
                EditorGUI.BeginChangeCheck();
                _frame = EditorGUILayout.IntSlider(_frame, 0, _currentFrame);
                if (EditorGUI.EndChangeCheck())
                {
                    _recording = false;
                    ReloadFrameData();
                }
                
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _frame = 0;
                    _assets.Clear();
                    _bundles.Clear();
                    ReloadFrameData();
                }
            } 
        }

        private void DrawTreeView(Rect rect)
        {
            _verticalSplitter.OnGUI(rect);
            if (_verticalSplitter.resizing)
            {
                Repaint();
            }
            
            var treeRect = new Rect(
                rect.xMin,
                rect.yMin + k_ToolbarHeight,
                rect.width,
                _verticalSplitter.rect.y - k_ToolbarHeight);

            _assetTreeView?.OnGUI(treeRect);

            var rect2 = new Rect(treeRect.x, _verticalSplitter.rect.y + 4, treeRect.width,
                rect.height - treeRect.yMax - 4);
            _bundleTreeView?.OnGUI(rect2);
        }
        
        private void TakeSample()
        {
            _currentFrame = _frame = Time.frameCount;
            
            _assets = new List<Loadable>();
            foreach (var item in Asset.Cache.Values)
            {
                if (item.isDone)
                {
                    _assets.Add(item);
                }
            }
            foreach (var item in RawAsset.Cache.Values)
            {
                if (item.isDone)
                {
                    _assets.Add(item);
                }
            }
            if (Scene.main != null && Scene.main.isDone)
            {
                _assets.Add(Scene.main);
                _assets.AddRange(Scene.main.additives);
            }
            _frameWithAssets[_frame] = _assets;
            
            _bundles = new List<Bundle>();
            foreach (var item in Bundle.Cache.Values)
            {
                if (item.isDone)
                {
                    _bundles.Add(item);
                }
            }
            _frameWithBundles[_frame] = _bundles;
            
            ReloadFrameData();
        }

        private void ReloadFrameData()
        {
            if (_assetTreeView != null)
            {
                _assetTreeView.SetAssets(
                    _frameWithAssets.TryGetValue(_frame, out var value) ? value : new List<Loadable>());
            }
            
            if (_bundleTreeView != null)
            {
                _bundleTreeView.SetBundles(
                    _frameWithBundles.TryGetValue(_frame, out var value) ? value : new List<Bundle>());
            }
        }

        public void ReloadBundleView(string assetPath)
        {
            
        }
    }
}