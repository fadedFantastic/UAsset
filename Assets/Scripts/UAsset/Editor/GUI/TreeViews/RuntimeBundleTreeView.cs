using System.Collections.Generic;
using System.IO;
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
        private static string _pathAlias = "Depend Bundles";
        private List<Bundle> _bundles = new List<Bundle>();

        private enum BundleColumn
        {
            Path,
            References
        }

        internal RuntimeBundleTreeView(TreeViewState state, MultiColumnHeaderState headerState, 
            RuntimeInfoWindow editor) : 
            base(state, new MultiColumnHeader(headerState))
        {
            _editor = editor;
            showBorder = true;
            showAlternatingRowBackgrounds = false;
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
                    headerContent = new GUIContent(_pathAlias),
                    minWidth = 900,
                    width = 900,
                    headerTextAlignment = TextAlignment.Left,
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
            return base.BuildRows(root);
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
                case BundleColumn.References:
                    DefaultGUI.Label(cellRect, item.data.referenceCount.ToString(), args.selected, args.focused);
                    break;
            }
        }
        
        protected override void SingleClickedItem(int id)
        {
            base.SingleClickedItem(id);
            
            var item = FindItem(id, rootItem) as RuntimeBundleTreeViewItem;
            _editor.ReloadAssetView(item?.data.pathOrURL);
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }
        
        public void SetAsMainView(bool setToMain)
        {
            showAlternatingRowBackgrounds = setToMain;
            _pathAlias = setToMain ? "Bundle Name" : "Depend Bundles";
            
            
        }

        public void SetBundles(List<Bundle> bundles)
        {
            _bundles = bundles;
            Reload();
        }
    }
}