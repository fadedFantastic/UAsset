using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly RuntimeInfoWindow _editor;
        private List<Loadable> _assets = new List<Loadable>();

        private enum AssetColumn
        {
            Path,           // 资源路径
            Size,           // 资源大小
            LoadScene,      // 被加载的场景
            LoadTimes,      // 加载次数
            UnloadTimes,    // 卸载次数
            Reference,      // 引用计数次数
        }
        
        private readonly AssetColumn[] _sortOptions =
        {
            AssetColumn.Path,
            AssetColumn.Size,
            AssetColumn.LoadScene,
            AssetColumn.LoadTimes,
            AssetColumn.UnloadTimes,
            AssetColumn.Reference,
        };
        
        internal RuntimeAssetTreeView(TreeViewState state, MultiColumnHeaderState headerState,
            RuntimeInfoWindow editor) : 
            base(state, new MultiColumnHeader(headerState))
        {
            _editor = editor;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            extraSpaceBeforeIconAndLabel = 5;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }
        
        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
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
                    headerContent = new GUIContent("Asset Path"),
                    minWidth = 500,
                    width = 500,
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
                    headerContent = new GUIContent("Reference"),
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
            foreach (var asset in _assets)
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
                var result = new List<TreeViewItem>();
                foreach (var current in rows)
                {
                    if (current.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(current);
                    }
                }

                rows = result;
            }

            SortIfNeeded(root, rows);
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
                case AssetColumn.Reference:
                    DefaultGUI.Label(cellRect, item.data.referenceCount.ToString(), args.selected, args.focused);
                    break;
            }
        }
        
        private static long SizeOf(Loadable loadable)
        {
            var file = new FileInfo(loadable.pathOrURL);
            return file.Exists ? file.Length : 0;
        }
        
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count > 0)
            {
                var item = FindItem(selectedIds[0], rootItem) as RuntimeAssetTreeViewItem;
                _editor.ReloadBundleView(item?.data);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetItem = FindItem(id, rootItem);
            if (assetItem != null)
            {
                var o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetItem.displayName);
                EditorGUIUtility.PingObject(o);
                Selection.activeObject = o;
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }
        
        private void OnSortingChanged(MultiColumnHeader header)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        private void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
                return;
			
            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return; // No column to sort for (just use the order the data are in)
            }

            SortByColumn();

            rows.Clear();
            foreach (var t in root.children)
            {
                rows.Add(t);
            }

            Repaint();
        }

        private void SortByColumn()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
            {
                return;
            }

            var assetList = new List<TreeViewItem>(rootItem.children);
            var orderedItems = InitialOrder(assetList, sortedColumns);
            rootItem.children = orderedItems.ToList();
        }

        private IEnumerable<TreeViewItem> InitialOrder(IEnumerable<TreeViewItem> myTypes, int[] history)
        {
            var sortOption = _sortOptions[history[0]];
            var ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case AssetColumn.Path:
                    return myTypes.Order(l => l.displayName, ascending);
                case AssetColumn.Size:
                    return myTypes.Order(l => SizeOf(((RuntimeAssetTreeViewItem)l).data), ascending);
                case AssetColumn.LoadScene:
                    return myTypes.Order(l => ((RuntimeAssetTreeViewItem)l).data.loadScene, ascending);
                case AssetColumn.LoadTimes:
                    return myTypes.Order(l => ((RuntimeAssetTreeViewItem)l).data.loadTimes, ascending);
                case AssetColumn.UnloadTimes:
                    return myTypes.Order(l => ((RuntimeAssetTreeViewItem)l).data.unloadTimes, ascending);
                case AssetColumn.Reference:
                    return myTypes.Order(l => ((RuntimeAssetTreeViewItem)l).data.referenceCount, ascending);
            }

            return myTypes.Order(l => new FileInfo(l.displayName).Length, ascending);
        }

        public void SetAsMainView(bool setToMain)
        {
            showAlternatingRowBackgrounds = setToMain;
            searchString = string.Empty;
            SetSelection(new List<int>());
            if (!setToMain) ClearView();
        }

        private void ClearView()
        {
            _assets.Clear();
            Reload();
        }
        
        public void SetAssets(IEnumerable<Loadable> assets)
        {
            _assets.Clear();
            _assets.AddRange(assets);
            Reload();
        }
    }
    
    internal static class ExtensionMethods
    {
        internal static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector,
            bool ascending)
        {
            return ascending ? source.OrderBy(selector) : source.OrderByDescending(selector);
        }
    }
}