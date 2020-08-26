using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using System.Collections.Generic;
using VRC.Udon.Graph.Interfaces;
using System.Linq;
using System;
using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{

    public class UdonSearchWindowBase : ScriptableObject, ISearchWindowProvider
    {
        // Reference to actual Graph View
        internal UdonGraph _graphView;
        private List<SearchTreeEntry> _exampleLookup;
        internal UdonGraphWindow _editorWindow;

        public virtual void Initialize(UdonGraphWindow editorWindow, UdonGraph graphView)
        {
            _editorWindow = editorWindow;
            _graphView = graphView;
        }

        #region ISearchWindowProvider

        public virtual List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (_exampleLookup != null && _exampleLookup.Count > 0) return _exampleLookup;

            _exampleLookup = new List<SearchTreeEntry>();

            Texture2D icon = EditorGUIUtility.FindTexture("cs Script Icon");
            _exampleLookup.Add(new SearchTreeGroupEntry(new GUIContent("Create Node"), 0));

            return _exampleLookup;
        }

        public virtual bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            return true;
        }

        #endregion

        internal Vector2 GetGraphPositionFromContext(SearchWindowContext context)
        {
            var windowRoot = _editorWindow.GetRootVisualContainer();
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, context.screenMousePosition - _editorWindow.position.position);
            var graphMousePosition = _graphView.contentViewContainer.WorldToLocal(windowMousePosition);
            return graphMousePosition;
        }

        internal void AddEntries(List<SearchTreeEntry> cache, IEnumerable<UdonNodeDefinition> definitions, int level, bool stripToLastDot = false)
        {
            Texture2D icon = EditorGUIUtility.FindTexture("cs Script Icon");
            Dictionary<string, UdonNodeDefinition> baseNodeDefinition = new Dictionary<string, UdonNodeDefinition>();

            foreach (UdonNodeDefinition nodeDefinition in definitions.OrderBy(s => UdonGraphExtensions.PrettyFullName(s)))
            {
                string baseIdentifier = nodeDefinition.fullName;
                string[] splitBaseIdentifier = baseIdentifier.Split(new[] { "__" }, StringSplitOptions.None);
                if (splitBaseIdentifier.Length >= 2)
                {
                    baseIdentifier = $"{splitBaseIdentifier[0]}__{splitBaseIdentifier[1]}";
                }
                if (baseNodeDefinition.ContainsKey(baseIdentifier))
                {
                    continue;
                }
                baseNodeDefinition.Add(baseIdentifier, nodeDefinition);
            }

            // add all subTypes
            foreach (KeyValuePair<string, UdonNodeDefinition> nodeDefinitionsEntry in baseNodeDefinition)
            {
                string nodeName = UdonGraphExtensions.PrettyBaseName(nodeDefinitionsEntry.Key);
                nodeName = nodeName.UppercaseFirst();
                nodeName = nodeName.Replace("_", " ");
                if (stripToLastDot)
                {
                    int lastDotIndex = nodeName.LastIndexOf('.');
                    nodeName = nodeName.Substring(lastDotIndex + 1);
                }

                // don't add Variable or Comment nodes
                if (nodeName.StartsWithCached("Variable") || nodeName.StartsWithCached("Get Var") || nodeName.StartsWithCached("Set Var") || nodeName.StartsWithCached("Comment"))
                {
                    continue;
                }
                if (nodeName.StartsWithCached("Object"))
                {
                    nodeName = $"{nodeDefinitionsEntry.Value.type.Namespace}.{nodeName}";
                }
                cache.Add(new SearchTreeEntry(new GUIContent(nodeName, icon)) { level = level, userData = nodeDefinitionsEntry.Value });
            }
        }

        // adds all entries so we can use this for regular and array registries
        internal void AddEntriesForRegistry(List<SearchTreeEntry> cache, INodeRegistry registry, int level, bool stripToLastDot = false)
        {
            AddEntries(cache, registry.GetNodeDefinitions(), level, stripToLastDot);
        }

    }
}