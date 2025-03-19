using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace MornEditor
{
    public abstract class MornEditorTreeBase<T> where T : new()
    {
        private readonly MornEditorNode _rootNode;

        private class MornEditorNode
        {
            private readonly MornEditorTreeBase<T> _tree;
            private readonly MornEditorNode _parentNode;
            private readonly string _originalPath;
            private readonly string _folderName;
            private readonly Dictionary<string, MornEditorNode> _childNodes = new();
            private readonly List<T> _childList = new();
            private bool isRoot => _parentNode == null;
            private bool _isFoldout;
            private int _indent;
            private int totalIndent => _parentNode?.totalIndent + _indent ?? _indent;

            public MornEditorNode(MornEditorTreeBase<T> tree, MornEditorNode parent, string originalPath)
            {
                _tree = tree;
                _parentNode = parent;
                _originalPath = originalPath;
                _folderName = originalPath.Substring(0, originalPath.Length - 1).Split('/').Last();
            }

            public void Add(T target)
            {
                var path = _tree.NodeToPath(target);
                Assert.IsTrue(path.StartsWith(_originalPath));
                var pathFromPrefix = path.Substring(_originalPath.Length);
                if (pathFromPrefix.Contains("/"))
                {
                    var childPath = pathFromPrefix.Substring(0, pathFromPrefix.IndexOf('/'));
                    if (!_childNodes.TryGetValue(childPath, out var childNode))
                    {
                        childNode = new MornEditorNode(_tree, this, _originalPath + childPath + "/");
                        _childNodes[childPath] = childNode;
                    }

                    childNode.Add(target);
                }
                else
                {
                    _childList.Add(target);
                }
            }

            public void OnGUI()
            {
                if (!isRoot)
                {
                    _isFoldout = FoldOutButton(_isFoldout, _folderName);
                    _indent++;
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(40 * totalIndent);
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        if (_isFoldout || isRoot)
                        {
                            foreach (var sceneNode in _childNodes.Values)
                            {
                                sceneNode.OnGUI();
                            }

                            foreach (var child in _childList)
                            {
                                var path = _tree.NodeToPath(child);
                                var pathFromPrefix = path.Substring(_originalPath.Length);
                                IndentButton(totalIndent, pathFromPrefix, () =>
                                {
                                    _tree.NodeOnGUI(child);
                                });
                            }
                        }
                    }
                }

                if (!isRoot)
                {
                    _indent--;
                }
            }
        }

        protected MornEditorTreeBase(string originalPath)
        {
            _rootNode = new MornEditorNode(this, null, originalPath);
        }

        public void Add(T node)
        {
            _rootNode.Add(node);
        }

        public void OnGUI()
        {
            _rootNode.OnGUI();
        }

        protected abstract string NodeToPath(T node);
        protected abstract void NodeOnGUI(T node);

        private static bool FoldOutButton(bool isFoldout, string text)
        {
            var label = (isFoldout ? "▼" : "▶") + text;
            var labelWidth = GUI.skin.label.CalcSize(new GUIContent(label)).x;
            if (GUILayout.Button(label, GUILayout.Width(labelWidth + 40)))
            {
                isFoldout = !isFoldout;
            }

            return isFoldout;
        }

        private static void IndentButton(int indent, string text, Action callback)
        {
            if (GUILayout.Button(text))
            {
                callback();
            }
        }
    }
}