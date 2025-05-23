using System;
using System.Collections.Generic;
using FSM.Editor.Attributes;
using FSM.Editor.Nodes;
using FSM.Editor.Ports;
using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Commands;
using UnityEditor;
using UnityEngine;

namespace FSM.Editor.Actions
{
    public abstract class FsmEditorActionBase : ISerialize
    {
        private readonly string _type;
        public bool Linked { get; private set; }
        private bool _cyclical;
        private readonly string _name;
        public string Category { get; private set; }

        protected virtual Dictionary<string, object> OutputEvents { get; } = new();

        private readonly List<string> _keys = new();
        public Dictionary<string, object> OutputEvent2PortId { get; } = new();

        protected FsmEditorState parentNode;
        protected RawNode node;

        public bool PendingUpdatingPorts { get; set; }
        public bool PendingUp { get; set; }
        public bool PendingDown { get; set; }
        public bool PendingDestroy { get; set; }

        protected bool hideCyclicalField;

        protected FsmEditorActionBase(FsmEditorState parent, RawNode node = null)
        {
            this.node = node;
            parentNode = parent;

            var attributes = GetType().GetCustomAttributes(typeof(FsmEditorActionAttribute), true);

            if (attributes.Length == 1)
            {
                var attrib = (FsmEditorActionAttribute)attributes[0];
                _type = attrib.Type;
                _name = attrib.Name;
                Category = attrib.Category;
            }
            else
            {
                throw new Exception("FsmEditorActionBase");
            }

            if (node != null)
            {
                Linked = node.GetBool("linked");
                _cyclical = node.GetBool("cc");
                OutputEvent2PortId = node.GetNode("portIds").ToDictionary();
            }
            
            InitOutputs();
        }

        private void InitOutputs()
        {
            _keys.AddRange(OutputEvents.Keys);
            
            if (node != null)
            {
                var outputsNode = node.GetNode("outputs");

                foreach (var key in _keys)
                {
                    OutputEvents[key] = outputsNode.GetBool(key);
                }
            }
        }

        public virtual void Draw(Rect iRect)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("↑", GUILayout.Width(20)))
            {
                PendingUp = true;
            }

            if (GUILayout.Button("↓", GUILayout.Width(20)))
            {
                PendingDown = true;
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                PendingDestroy = true;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Name", _name, GUILayout.Width(iRect.width));
            var linked = EditorGUILayout.Toggle("Linked", Linked, GUILayout.Width(iRect.width));

            if (parentNode.SerializeContentIfChanged(linked, Linked))
            {
                Linked = linked;
            }

            if (!hideCyclicalField)
            {
                var tmp = GUI.color;
                GUI.color = Color.cyan;
                
                var cyclical = EditorGUILayout.Toggle("Cyclical", _cyclical, GUILayout.Width(iRect.width));

                if (parentNode.SerializeContentIfChanged(cyclical, _cyclical))
                {
                    _cyclical = cyclical;
                }
                
                GUI.color = tmp;
            }

            EditorGUILayout.Separator();

            foreach (var key in _keys)
            {
                var v = (bool)OutputEvents[key];
                var value = EditorGUILayout.Toggle(key, v, GUILayout.Width(iRect.width));

                if (parentNode.SerializeContentIfChanged(value, v))
                {
                    OutputEvents[key] = value;

                    if (value)
                    {
                        var port = new DebuggingConnectionPort(
                            parentNode,
                            key,
                            new Vector2(0, 0),
                            PortSide.Right,
                            PortIcon.Right,
                            PortDirection.Out,
                            false,
                            new Color32(50, 150, 50, 255),
                            "state");

                        var comm = parentNode.Editor.DoCommand(new CreateConnectionPortCommand(port, parentNode.Editor));
                        comm.UndoPrev = true;

                        OutputEvent2PortId.Add(key, port.Id);
                        PendingUpdatingPorts = true;
                    }
                    else
                    {
                        var portId = (string)OutputEvent2PortId[key];

                        var port = parentNode.FindPortById(portId);
                        var comm = parentNode.Editor.DoCommand(new DestroyConnectionPortCommand(port, parentNode.Editor));
                        comm.UndoPrev = true;

                        OutputEvent2PortId.Remove(key);
                        PendingUpdatingPorts = true;
                    }

                    break;
                }
            }
            
            EditorGUILayout.Separator();
        }

        public virtual object Serialize()
        {
            return SerializeUtil.Dict().SetArgs(
                "type", _type,
                "linked", Linked,
                "cc", _cyclical, 
                "outputs", new Dictionary<string, object>(OutputEvents), //чтобы не тот же самый словарь вернулся в RawNode
                "portIds", new Dictionary<string, object>(OutputEvent2PortId) //чтобы не тот же самый словарь вернулся в RawNode
            );
        }

        public virtual object SerializeRuntime()
        {
            var dict = SerializeUtil.Dict().SetArgs(
                "type", _type,
                "cc", _cyclical,
                "linked", Linked
            );

            var to = new Dictionary<string, object>();

            foreach (var pair in OutputEvents)
            {
                if ((bool)pair.Value)
                {
                    var port = parentNode.FindPortById((string)OutputEvent2PortId[pair.Key]);
                    var conn = parentNode.Editor.FindConnection(port);
                    if (conn != null)
                    {
                        to.Add(pair.Key, conn.InPort.ParentNode.Id);
                    }
                }
            }

            if (to.Count > 0)
            {
                dict.Add("to", to);
            }

            return dict;
        }
    }
}