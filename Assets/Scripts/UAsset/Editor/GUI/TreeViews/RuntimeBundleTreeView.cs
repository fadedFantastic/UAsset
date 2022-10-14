using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UAsset.Editor
{
    public sealed class RuntimeBundleTreeViewItem : TreeViewItem
    {
        public readonly Bundle data;

        public RuntimeBundleTreeViewItem(Bundle bundle, int depth) : base(bundle.pathOrURL.GetHashCode(), depth)
        {
            displayName = Path.GetFileName(bundle.pathOrURL);
            data = bundle;
        }
    }
    
    public class RuntimeBundleTreeView : TreeView
    {
        private readonly RuntimeInfoWindow _editor;
        private List<Bundle> _bundles = new List<Bundle>();

        private enum BundleColumn
        {
            Path,           // 资源路径
            Size,           // 资源大小
            LoadScene,      // 被加载的场景
            LoadTimes,      // 加载次数
            UnloadTimes,    // 卸载次数
            Reference       // 引用计数次数
        }
        
        private readonly BundleColumn[] _sortOptions =
        {
            BundleColumn.Path,
            BundleColumn.Size,
            BundleColumn.LoadScene,
            BundleColumn.LoadTimes,
            BundleColumn.UnloadTimes,
            BundleColumn.Reference,
        };

        internal RuntimeBundleTreeView(TreeViewState state, MultiColumnHeaderState headerState, 
            RuntimeInfoWindow editor) : 
            base(state, new MultiColumnHeader(headerState))
        {
            _editor = editor;
            showBorder = true;
            showAlternatingRowBackgrounds = false;
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
                    headerContent = new GUIContent("Bundle Name"),
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
                },
            };
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            foreach (var bundle in _bundles)
            {
                root.AddChild(new RuntimeBundleTreeViewItem(bundle, 0));
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
                var item = (RuntimeBundleTreeViewItem)args.item;
                if (item == null || item.data == null)
                {
                    using (new EditorGUI.DisabledScope())
                    {
                        base.RowGUI(args);
                    }
                }
                else
                {
                    CellGUI(args.GetCellRect(i), item, (BundleColumn)args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, RuntimeBundleTreeViewItem item, BundleColumn column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case BundleColumn.Path:
                    DefaultGUI.Label(cellRect, item.displayName, args.selected, args.focused);
                    break;
                case BundleColumn.Size:
                    DefaultGUI.Label(cellRect, EditorUtility.FormatBytes(item.data.bundleInfo.size), args.selected, args.focused);
                    break;
                case BundleColumn.LoadScene:
                    DefaultGUI.Label(cellRect, item.data.loadScene, args.selected, args.focused);
                    break;
                case BundleColumn.LoadTimes:
                    DefaultGUI.Label(cellRect, item.data.loadTimes.ToString(), args.selected, args.focused);
                    break;
                case BundleColumn.UnloadTimes:
                    DefaultGUI.Label(cellRect, item.data.unloadTimes.ToString(), args.selected, args.focused);
                    break;
                case BundleColumn.Reference:
                    DefaultGUI.Label(cellRect, item.data.referenceCount.ToString(), args.selected, args.focused);
                    break;
            }
        }
        
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count > 0)
            {
                var item = FindItem(selectedIds[0], rootItem) as RuntimeBundleTreeViewItem;
                _editor.ReloadAssetView(item?.data.pathOrURL);
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
                case BundleColumn.Path:
                    return myTypes.Order(l => l.displayName, ascending);
                case BundleColumn.Size:
                    return myTypes.Order(l => ((RuntimeBundleTreeViewItem)l).data.bundleInfo.size, ascending);
                case BundleColumn.LoadScene:
                    return myTypes.Order(l => ((RuntimeBundleTreeViewItem)l).data.loadScene, ascending);
                case BundleColumn.LoadTimes:
                    return myTypes.Order(l => ((RuntimeBundleTreeViewItem)l).data.loadTimes, ascending);
                case BundleColumn.UnloadTimes:
                    return myTypes.Order(l => ((RuntimeBundleTreeViewItem)l).data.unloadTimes, ascending);
                case BundleColumn.Reference:
                    return myTypes.Order(l => ((RuntimeBundleTreeViewItem)l).data.referenceCount, ascending);
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
            _bundles.Clear();
            Reload();
        }
        
        public void SetBundles(List<Bundle> bundles)
        {
            _bundles = bundles;
            Reload();
        }
    }
}