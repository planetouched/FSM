using System;
using System.Collections.Generic;
using FSM.Editor.Attributes;
using FSM.Editor.Actions;
using FSM.Editor.Ports;
using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Undo;
using NodeLib.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace FSM.Editor.Nodes
{
    public class FsmEditorState : FsmDebuggingNode
    {
        public const string Type = "state";
        protected override string NodeType => Type;

        public override Vector2 MinSize => new(120, 64);

        private bool _parallel;
        private readonly List<FsmEditorActionBase> _actions = new();

        private int _selectedIdx;
        private string _filter = String.Empty;

        private static readonly string[] _categories;

        private int _categorySelectedIdx;

        private string[] _filteredItems;
        private int[] _filteredIndexes;

        private static readonly List<FsmEditorActionAttribute> _attributes = new();

        private GUIStyle _separatorStyle;
        private Vector2 _scrollPos;

        static FsmEditorState()
        {
            var cats = new List<string> { "<All>" };

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = type.GetCustomAttributes(typeof(FsmEditorActionAttribute), false);
                    if (attributes.Length != 1) continue;
                    var attrib = (FsmEditorActionAttribute)attributes[0];
                    attrib.ActionType = type;
                    _attributes.Add(attrib);

                    if (!cats.Contains(attrib.Category))
                    {
                        cats.Add(attrib.Category);
                    }
                }
            }

            _categories = cats.ToArray();
        }

        public FsmEditorState(string title, Vector2 position) : base(title, position)
        {
            _separatorStyle = new GUIStyle();
            _separatorStyle.normal.background = Texture2D.whiteTexture;
            
            Ports.Add(new DebuggingConnectionPort(
                this,
                "In",
                new Vector2(0, 16),
                PortSide.Left,
                PortIcon.Right,
                PortDirection.In,
                true,
                Color.green,
                "state"));
            
            Ports.Add(new DebuggingConnectionPort(
                this,
                "In1",
                new Vector2(Size.x / 2, 0),
                PortSide.Up,
                PortIcon.Down,
                PortDirection.In,
                true,
                Color.green,
                "state"));
            
            Ports.Add(new DebuggingConnectionPort(
                this,
                "Finish",
                new Vector2(0, 16),
                PortSide.Right,
                PortIcon.Right,
                PortDirection.Out,
                false,
                Color.green,
                "state"));
        }

        public FsmEditorState(RawNode data) : base(data)
        {
        }

        protected override void OnInitialize(RawNode rawData)
        {
            _separatorStyle = new GUIStyle();
            _separatorStyle.normal.background = Texture2D.whiteTexture;
            
            _actions.Clear();

            foreach (var actNode in rawData.GetRawNodeArray("actions"))
            {
                var type = actNode.GetString("type");
                foreach (var attrib in _attributes)
                {
                    if (type == attrib.Type)
                    {
                        var action = (FsmEditorActionBase)Activator.CreateInstance(attrib.ActionType, this, actNode);
                        _actions.Add(action);
                        break;
                    }
                }
            }

            _parallel = rawData.GetBool("parallel");
        }

        private string[] Filtered()
        {
            if (_filteredItems != null)
            {
                return _filteredItems;
            }

            var list = new List<string>();
            var listIdx = new List<int>();
            var lower = _filter.ToLower();
            var category = _categories[_categorySelectedIdx];

            for (var i = 0; i < _attributes.Count; i++)
            {
                var attrib = _attributes[i];

                if (category == "<All>" || attrib.Category == category)
                {
                    if (string.IsNullOrEmpty(lower) || attrib.Name.ToLower().Contains(lower))
                    {
                        listIdx.Add(i);
                        list.Add(attrib.Name);
                    }
                }
            }

            _filteredItems = list.ToArray();
            _filteredIndexes = listIdx.ToArray();

            return _filteredItems;
        }

        public override void DrawInspector(Rect iRect)
        {
            base.DrawInspector(iRect);

            var parallel = EditorGUILayout.Toggle("Parallel", _parallel, GUILayout.Width(iRect.width));

            if (SerializeContentIfChanged(parallel, _parallel))
            {
                _parallel = parallel;
            }

            EditorGUILayout.Separator();

            var filter = EditorGUILayout.TextField("Filter", _filter, GUILayout.Width(iRect.width));
            if (filter != _filter)
            {
                _filter = filter;
                _selectedIdx = 0;
                _filteredItems = null;
            }

            var categorySelectedIdx = EditorGUILayout.Popup("Category", _categorySelectedIdx, _categories, GUILayout.Width(iRect.width));
            if (categorySelectedIdx != _categorySelectedIdx)
            {
                _categorySelectedIdx = categorySelectedIdx;
                _selectedIdx = 0;
                _filteredItems = null;
            }

            GUILayout.BeginHorizontal();
            _selectedIdx = EditorGUILayout.Popup("Select Action", _selectedIdx, Filtered(), GUILayout.Width(iRect.width - 30));

            if (_filteredItems.Length > 0 && GUILayout.Button("+", GUILayout.Width(30)))
            {
                SerializeContent();
                var attrib = _attributes[_filteredIndexes[_selectedIdx]];
                var action = (FsmEditorActionBase)Activator.CreateInstance(attrib.ActionType, this, null);
                _actions.Add(action);
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            if (_actions.Count > 0)
            {
                NodeLibUtil.DrawWithColor(new Color(0.3f, 0.3f, 0.3f), () => EditorGUILayout.LabelField("", GUI.skin.horizontalSlider));
            }

            bool updatePorts = false;
            FsmEditorActionBase up = null;
            FsmEditorActionBase down = null;
            FsmEditorActionBase destroy = null;
            
            EditorGUILayout.BeginVertical();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(iRect.width + 20), GUILayout.Height(iRect.height - 150));

            var counter = 0;
            var actionStyle = new GUIStyle();
            actionStyle.normal.background = Texture2D.whiteTexture;
            var oddColor = new Color32(60, 60, 60, 255);
            var evenColor = new Color32(120, 120, 120, 255);

            for (var i = 0; i < _actions.Count; i++)
            {
                var bc = GUI.backgroundColor;
                GUI.backgroundColor = counter++ % 2 == 0 ? oddColor : evenColor;
                EditorGUILayout.BeginVertical(actionStyle);
                EditorGUILayout.Separator();
                GUI.backgroundColor = bc;

                var action = _actions[i];
                action.Draw(iRect);

                if (action.PendingUpdatingPorts)
                {
                    updatePorts = true;
                }

                if (action.PendingUp)
                {
                    up = action;
                    action.PendingUp = false;
                }

                if (action.PendingDown)
                {
                    down = action;
                    action.PendingDown = false;
                }

                if (action.PendingDestroy)
                {
                    destroy = action;
                    action.PendingDestroy = false;
                }

                if (i < _actions.Count - 1)
                {
                    if (_parallel || action.Linked && _actions[i + 1].Linked)
                    {
                        NodeLibUtil.DrawWithColor(new Color(0f, 1f, 0f), () => EditorGUILayout.LabelField("", GUI.skin.horizontalSlider));
                        NodeLibUtil.DrawWithColor(new Color(0f, 1f, 0f), () => EditorGUILayout.LabelField("", GUI.skin.horizontalSlider));
                    }
                    else
                    {
                        //NodeLibUtil.DrawWithColor(new Color(0.3f, 0.3f, 0.3f), () => EditorGUILayout.LabelField("", GUI.skin.horizontalSlider));
                    }
                }

                EditorGUILayout.Separator();
                EditorGUILayout.EndVertical();
            }
            
            NodeLibUtil.DrawWithColor(new Color(0.3f, 0.3f, 0.3f), () => EditorGUILayout.LabelField("", GUI.skin.horizontalSlider));

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (destroy != null)
            {
                SerializeContent();
                _actions.Remove(destroy);
                foreach (var pair in destroy.OutputEvent2PortId)
                {
                    var port = FindPortById((string)pair.Value);
                    var comm = Editor.DoCommand(new DestroyConnectionPortCommand(port, Editor));
                    comm.UndoPrev = true;
                }
                updatePorts = true;
            }
            
            if (up != null)
            {
                int idx = _actions.IndexOf(up);
                if (idx > 0)
                {
                    SerializeContent();
                    _actions.RemoveAt(idx);
                    _actions.Insert(idx - 1, up);
                    updatePorts = true;
                }
            }
            
            if (down != null)
            {
                int idx = _actions.IndexOf(down);
                if (idx < _actions.Count - 1)
                {
                    SerializeContent();
                    _actions.RemoveAt(idx);
                    _actions.Insert(idx + 1, down);
                    updatePorts = true;
                }
            }
            
            if (updatePorts)
            {
                float offset = 0;
                
                foreach (var action in _actions)
                {
                    foreach (var pair in action.OutputEvent2PortId)
                    {
                        var port = FindPortById((string)pair.Value);
                        port.SideOffset = new Vector2(0, offset + 48);
                        offset += 20;
                    }

                    action.PendingUpdatingPorts = false;
                }
                
                Size = new Vector2(Size.x, HeaderHeight + offset - 4 + 48);
                UpdateSize();
            }
        }

        protected override void OnSizeChange()
        {
            var port = FindPortByName("In1");
            if (port != null)
            {
                port.SideOffset = new Vector2(Size.x / 2, 0);
            }
        }

        public override void Draw()
        {
            base.Draw();

            float posY = HeaderHeight + 5 + 32;
            var color = new Color(0.7f, 0.7f, 0.7f);

            if (_actions.Count > 0)
            {
                foreach (var action in _actions)
                {
                    if (action.OutputEvent2PortId.Count == 0) continue;
                
                    posY += action.OutputEvent2PortId.Count * 20;
                    var y = posY;

                    if (_actions[_actions.Count - 1] != action)
                    {
                        NodeLibUtil.DrawWithColor(color, () => GUI.Box(new Rect(Position.x, Position.y + y, Size.x, 2), string.Empty, _separatorStyle));
                    }
                }
            }
        }

        public override object Serialize()
        {
            return SerializeUtil.Dict(base.Serialize()).SetArgs("parallel", _parallel, "actions", SerializeUtil.SerializeArray(_actions));
        }

        public virtual object SerializeRuntime()
        {
            var actions = new List<object>();
            
            foreach (var action in _actions)
            {
                actions.Add(action.SerializeRuntime());
            }

            var dict = SerializeUtil.Dict().SetArgs("parallel", _parallel, "actions", actions);
            
            
            var port = FindPortByName("Finish");
            var conn = Editor.FindConnection(port);
            if (conn != null)
            {
                dict.Add("finish", conn.InPort.ParentNode.Id);
            }
            
            return dict;
        }
    }
}