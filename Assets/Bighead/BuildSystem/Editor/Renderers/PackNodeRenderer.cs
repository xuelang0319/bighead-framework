#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>递归渲染节点树</summary>
    public class PackNodeRenderer
    {
        public void DrawTree(IReadOnlyList<PackFolderNode> roots)
        {
            foreach (var root in roots)
            {
                DrawNode(root);
            }
        }

        private void DrawNode(PackNode node)
        {
            if (node is PackFolderNode folder)
            {
                folder.IsExpanded = EditorGUILayout.Foldout(folder.IsExpanded, folder.Name);
                if (folder.IsExpanded)
                {
                    EditorGUI.indentLevel++;
                    foreach (var child in folder.Children)
                        DrawNode(child);
                    EditorGUI.indentLevel--;
                }
            }
            else if (node is PackFileNode file)
            {
                EditorGUILayout.LabelField(file.Name);
            }
        }
    }
}
#endif