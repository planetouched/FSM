using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FSM.Editor.Commands;
using FSM.Editor.Nodes;
using FSM.Editor.Ports;
using FSM.Runtime.Debugging;
using FSM.Runtime.Nodes;
using Jsonite;
using JsonRawNode;
using NodeLib.Editor;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Commands._Base;
using UnityEditor;
using UnityEngine;

namespace FSM.Editor
{
    public class FsmEditor : NodeLibEditor
    {
        private const string FsmEditorTempFileName = "FsmEditorTempFileName";

        protected override string EditorTypeKey => "Fsm";
        protected override string TempFileNameKey => FsmEditorTempFileName;
        protected override string LastOpenedFileNameKey => "FsmLastOpenedFileName";

        private static FsmEditor _editor;
        private Vector2 _lastMenuPosition;
        
        private static FsmEditorState _cloneNode;
        
        [MenuItem("Assets/Fsm/Open Fsm")]
        public static void QuestSchemeLoad()
        {
            if (Selection.assetGUIDs.Length != 1)
            {
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            var fullPath = Application.dataPath + "/../" + path;

            _editor = CreateWindow<FsmEditor>("FSM");
            _editor.OpenFile(fullPath);
        }

        [MenuItem("DevTools/FSM/New")]
        public static void CreateWindow()
        {
            EditorPrefs.DeleteKey(FsmEditorTempFileName);
            CreateWindow<FsmEditor>("FSM");
        }

        protected override ICommand CommandFactory(RawNode commData)
        {
            var cmd =  base.CommandFactory(commData);
            if (cmd != null) return cmd;
            
            var type = commData.GetString("type");
            switch (type)
            {
                case CloneNodeCommand.ConstType:
                    return new CloneNodeCommand(this, commData);
            }

            return null;
        }


        private void OnExitAction(object obj)
        {
        }

        private void OnEnterAction(object obj)
        {
        }
        
        private void OnExitWithEvent(object obj)
        {
            var pair = ((FsmState state, string eventName))obj;
            
            foreach (var node in Nodes)
            {
                if (node is FsmDebuggingNode debugNode && debugNode.Id == pair.state.Id)
                {
                    var port = node.FindPortByName(pair.eventName) as DebuggingConnectionPort;
                    port?.SetDebugMarker();
                    return;
                }
            }
        }        

        private void OnExitState(object obj)
        {
            foreach (var node in Nodes)
            {
                if (node is FsmDebuggingNode debugNode && debugNode.DebugActive)
                {
                    debugNode.DebugActive = false;
                    debugNode.SetDebugMarker();
                    return;
                }
            }
        }
        
        private void OnEnterState(object obj)
        {
            var stateGuid = (string)obj;

            foreach (var node in Nodes)
            {
                if (node is FsmDebuggingNode debugNode)
                {
                    debugNode.DebugActive = false;
                }
            }
            
            foreach (var node in Nodes)
            {
                if (node is FsmDebuggingNode debugNode && debugNode.Id == stateGuid)
                {
                    debugNode.DebugActive = true;
                    return;
                }
            }
        }        
        
        protected override void Draw()
        {
            ProcessMenuEvents();

            if (GUI.Button(new Rect(10, 10, 100, 20), "Go to Start"))
            {
                ContentOffset = Vector2.zero;
            }

            var e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (e.control && e.shift && e.keyCode == KeyCode.D)
                {
                    var focused = FindFocusedNode();
                    if (focused != null)
                    {
                        var from = (FsmEditorState)focused;
                        var cmd = new CloneNodeCommand(from, this);
                        cmd.Position = from.Position + Vector2.one * 100;
                        DoCommand(cmd);
                    }
                
                    e.Use();
                }

                if (e.control && e.shift && e.keyCode == KeyCode.C)
                {
                    _cloneNode = (FsmEditorState)FindFocusedNode();
                    e.Use();
                }
                
                if (e.control && e.shift && e.keyCode == KeyCode.V)
                {
                    if (_cloneNode != null)
                    {
                        var cmd = new CloneNodeCommand(_cloneNode, this);
                        cmd.Position = -_editor.ContentOffset + Vector2.one * 200;
                        DoCommand(cmd);
                    }
                
                    e.Use();
                }
            }
            
            base.Draw();
        }

        private void ProcessMenuEvents()
        {
            var e = Event.current;

            var inspectorRect = new Rect(position.width - InspectorWidth, 0, InspectorWidth, position.height);

            if (e.type == EventType.MouseDown && e.button == 1 && !inspectorRect.Contains(e.mousePosition))
            {
                if (FindNodeUnderCursor() != null) return;

                _lastMenuPosition = e.mousePosition - _editor.ContentOffset;

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Input"), false, CreateInputNode);
                menu.AddItem(new GUIContent("State"), false, CreateStateNode);
                menu.ShowAsContext();
            }
        }

        private void CreateStateNode()
        {
            CreateNode(new FsmEditorState("Untitled", _lastMenuPosition));
        }

        private void CreateInputNode()
        {
            CreateNode(new FsmEditorInputEvent("Untitled", _lastMenuPosition));
        }

        public override NodeBase NodeFactory(RawNode nodeData)
        {
            var type = nodeData.GetString("type");

            return type switch
            {
                FsmEditorState.Type => new FsmEditorState(nodeData),
                FsmEditorInputEvent.Type => new FsmEditorInputEvent(nodeData),
                _ => throw new Exception("NodeFactory")
            };
        }

        protected override void OnWindowEnable()
        {
            _editor = this;
            //KeepUndoHistoryInDocument = true;
            
            FsmDebugEvent.Attach(FsmDebugEvent.EnterState, OnEnterState);
            FsmDebugEvent.Attach(FsmDebugEvent.ExitState, OnExitState);
            FsmDebugEvent.Attach(FsmDebugEvent.EnterAction, OnEnterAction);
            FsmDebugEvent.Attach(FsmDebugEvent.ExitAction, OnExitAction);
            FsmDebugEvent.Attach(FsmDebugEvent.ExitWithEvent, OnExitWithEvent);
        }

        protected override void OnWindowDisable()
        {
            FsmDebugEvent.DetachFromLocalSubscriptions();
        }

        protected override void OnWindowDestroy()
        {
        }

        private static string CreateMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }

        protected override void OnSaveFile(string file)
        {
            var body = Json.Serialize(SerializeRuntime());
            var md5 = CreateMD5(body);

            var fileName = Path.GetFileNameWithoutExtension(file);
            var path = Path.GetDirectoryName(file);

            File.WriteAllText(path + "/" + fileName + ".rtm.json", body);
            File.WriteAllText(path + "/" + fileName + ".md5.txt", md5);
        }

        private object SerializeRuntime()
        {
            var dict = new Dictionary<string, object>();
            var inputs = new Dictionary<string, object>();
            var states = new Dictionary<string, object>();

            dict.Add("type", "fsm-runtime");
            dict.Add("inputs", inputs);
            dict.Add("states", states);

            foreach (var node in Nodes)
            {
                if (node is FsmEditorInputEvent input)
                {
                    var obj = input.SerializeRuntime();
                    if (obj == null) continue;
                    inputs.Add(node.Id, input.SerializeRuntime());
                }

                if (node is FsmEditorState state)
                {
                    states.Add(node.Id, state.SerializeRuntime());
                }
            }

            return dict;
        }
    }
}