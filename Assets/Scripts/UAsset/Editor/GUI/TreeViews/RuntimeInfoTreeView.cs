using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UAsset.Editor
{

    public sealed class RuntimeInfoTreeViewItem : TreeViewItem
    {
        public readonly Loadable data;

        public RuntimeInfoTreeViewItem(Loadable loadable, int depth) : base(loadable.pathOrURL.GetHashCode(), depth)
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
    
    public class RuntimeInfoTreeView : TreeView
    {

        private List<Loadable> assets = new List<Loadable>();

        internal RuntimeInfoTreeView(TreeViewState state, MultiColumnHeaderState headerState) : base(state,
            new MultiColumnHeader(headerState))
        {
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
                    minWidth = 300,
                    width = 350,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size"),
                    minWidth = 32,
                    width = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Loads"),
                    minWidth = 32,
                    width = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Unloads"),
                    minWidth = 60,
                    width = 70,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("References"),
                    minWidth = 80,
                    width = 100,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                }
            };
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            foreach (var asset in assets)
            {
                root.AddChild(new RuntimeInfoTreeViewItem(asset, 0));
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            // TODO: 搜索显示
            return base.BuildRows(root);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var item = (RuntimeInfoTreeViewItem)args.item;
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

        private void CellGUI(Rect cellRect, RuntimeInfoTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case 0:
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
                case 1:
                    DefaultGUI.Label(cellRect, UnityEditor.EditorUtility.FormatBytes(SizeOf(item.data)), args.selected, args.focused);
                    break;
                case 2:
                    DefaultGUI.Label(cellRect, item.data.loadTimes.ToString(), args.selected, args.focused);
                    break;
                case 3:
                    DefaultGUI.Label(cellRect, item.data.unloadTimes.ToString(), args.selected, args.focused);
                    break;
                case 4:
                    DefaultGUI.Label(cellRect, item.data.referenceCount.ToString(), args.selected, args.focused);
                    break;
            }
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