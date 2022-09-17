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
        [SerializeField] private MultiColumnHeaderState m_MultiColumnHeaderState;
        [SerializeField] private TreeViewState m_TreeViewState;

        private List<Loadable> loadables = new List<Loadable>();

        private readonly Dictionary<int, List<Loadable>> frameWithLoadables = new Dictionary<int, List<Loadable>>();

        private RuntimeInfoTreeView m_TreeView;
        
        private int frame;
        
        [MenuItem(MenuItems.kUAssetToolMenu + "运行时信息面板", false, 55)]
        private static void ShowWindow()
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
            if (m_TreeView == null)
            {
                m_TreeViewState = new TreeViewState();
                var headerState = RuntimeInfoTreeView.CreateMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                }

                m_MultiColumnHeaderState = headerState;
                m_TreeView = new RuntimeInfoTreeView(m_TreeViewState, headerState);
                m_TreeView.SetAssets(loadables);
            }
            
            // var treeRect = GUILayoutUtility.GetLastRect();
            // m_TreeView.OnGUI(new Rect(0, treeRect.yMax, position.width, position.height - treeRect.yMax));
            
            m_TreeView.OnGUI(new Rect(0, 0, position.width, position.height));
        }

        private void TakeSample()
        {
            frame = Time.frameCount;

            loadables = new List<Loadable>();

            foreach (var item in Asset.Cache.Values)
            {
                if (item.isDone)
                {
                    loadables.Add(item);
                }
            }

            foreach (var item in Bundle.Cache.Values)
            {
                if (item.isDone)
                {
                    loadables.Add(item);
                }
            }
            
            foreach (var item in RawAsset.Cache.Values)
            {
                if (item.isDone)
                {
                    loadables.Add(item);
                }
            }
            
            // TODO:场景

            frameWithLoadables[frame] = loadables;
            ReloadFrameData();
        }

        private void ReloadFrameData()
        {
            if (m_TreeView != null)
            {
                m_TreeView.SetAssets(
                    frameWithLoadables.TryGetValue(frame, out var value) ? value : new List<Loadable>());
            }
        }
    }
}