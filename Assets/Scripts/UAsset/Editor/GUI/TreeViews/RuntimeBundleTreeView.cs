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
        private List<Bundle> _bundles = new List<Bundle>();
        
        internal RuntimeBundleTreeView(TreeViewState state, MultiColumnHeaderState headerState) : base(state,
            new MultiColumnHeader(headerState))
        {
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
                    headerContent = new GUIContent("Depend Bundles"),
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
                    CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, RuntimeBundleTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case 0:
                    DefaultGUI.Label(cellRect, item.displayName, args.selected, args.focused);
                    break;
                case 1:
                    DefaultGUI.Label(cellRect, item.data.referenceCount.ToString(), args.selected, args.focused);
                    break;
            }
        }

        public void SetBundles(List<Bundle> loadables)
        {
            _bundles = loadables;
            Reload();
        }
    }
}