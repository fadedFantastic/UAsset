using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UAsset.Editor
{

    public sealed class RuntimeAssetTreeViewItem : TreeViewItem
    {
        public readonly Loadable data;

        public RuntimeAssetTreeViewItem(Loadable loadable, int depth) : base(loadable.pathOrURL.GetHashCode(), depth)
        {
            if (loadable.pathOrURL.StartsWith("Assets/"))
            {
                displayName = loadable.pathOrURL;
                icon = AssetDatabase.GetCachedIcon(displayName) as Texture2D;
            }
            else
            {
                displayName = Path.GetFileName(loadable.pathOrURL);
            }

            data = loadable;
        }
    }
    
    public class RuntimeAssetTreeView : TreeView
    {
        private RuntimeInfoWindow _editor;
        private List<Loadable> assets = new List<Loadable>();

        private readonly List<TreeViewItem> result = new List<TreeViewItem>();

        private enum AssetColumn
        {
            Path,           // 资源路径
            Size,           // 资源大小
            LoadScene,      // 被加载的场景
            LoadTimes,      // 加载次数
            UnloadTimes,    // 卸载次数
            References,     // 引用计数次数
        }
        
        internal RuntimeAssetTreeView(TreeViewState state, MultiColumnHeaderState headerState,
            RuntimeInfoWindow editor) : 
            base(state, new MultiColumnHeader(headerState))
        {
            _editor = editor;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            extraSpaceBeforeIconAndLabel = 5;
        }
        
        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                SetSelection(Array.Empty<int>(), TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        internal static MultiColumnHeaderState CreateMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(CreateColumn());
        }

        private static MultiColumnHeaderState.Column[] CreateColumn()
        {
            return new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    minWidth = 600,
                    width = 600,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size"),
                    minWidth = 100,
                    width = 100,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("LoadScene"),
                    minWidth = 100,
                    width = 100,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("LoadTimes"),
                    minWidth = 100,
                    width = 100,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("UnloadTimes"),
                    minWidth = 100,
                    width = 100,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("References"),
                    minWidth = 100,
                    width = 100,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = false
                }
            };
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            foreach (var asset in assets)
            {
                root.AddChild(new RuntimeAssetTreeViewItem(asset, 0));
            }
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            if (!string.IsNullOrEmpty(searchString))
            {
                result.Clear();
                var stack = new Stack<TreeViewItem>();
                foreach (var item in rows)
                {
                    stack.Push(item);
                }

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (current.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(current);
                    }

                    if (current.children != null && current.children.Count > 0)
                    {
                        foreach (var item in current.children)
                        {
                            stack.Push(item);
                        }
                    }
                }

                rows = result;
            }

            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var item = args.item as RuntimeAssetTreeViewItem;
                if (item?.data == null)
                {
                    using (new EditorGUI.DisabledScope())
                    {
                        base.RowGUI(args);
                    }
                }
                else
                {
                    CellGUI(args.GetCellRect(i), item, (AssetColumn)args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, RuntimeAssetTreeViewItem item, AssetColumn column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case AssetColumn.Path:
                    var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                    if (item.icon != null)
                    {
                        GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);
                    }

                    var content = item.displayName;
                    DefaultGUI.Label(
                        new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width, cellRect.height), 
                        content, 
                        args.selected, 
                        args.focused);
                    
                    break;
                case AssetColumn.Size:
                    DefaultGUI.Label(cellRect, EditorUtility.FormatBytes(SizeOf(item.data)), args.selected, args.focused);
                    break;
                case AssetColumn.LoadScene:
                    DefaultGUI.Label(cellRect, item.data.loadScene, args.selected, args.focused);
                    break;
                case AssetColumn.LoadTimes:
                    DefaultGUI.Label(cellRect, item.data.loadTimes.ToString(), args.selected, args.focused);
                    break;
                case AssetColumn.UnloadTimes:
                    DefaultGUI.Label(cellRect, item.data.unloadTimes.ToString(), args.selected, args.focused);
                    break;
                case AssetColumn.References:
                    DefaultGUI.Label(cellRect, item.data.referenceCount.ToString(), args.selected, args.focused);
                    break;
            }
        }

        protected override void SingleClickedItem(int id)
        {
            base.SingleClickedItem(id);
            
            var item = FindItem(id, rootItem) as RuntimeAssetTreeViewItem;
            _editor.ReloadBundleView(item?.data.pathOrURL);
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public void SetAssets(List<Loadable> loadables)
        {
            assets = loadables;
            Reload();
        }
        
        private static long SizeOf(Loadable loadable)
        {
            if (loadable is Bundle bundle)
            {
                return bundle.bundleInfo.size;
            }

            var file = new FileInfo(loadable.pathOrURL);
            return file.Exists ? file.Length : 0;
        }
    }
}