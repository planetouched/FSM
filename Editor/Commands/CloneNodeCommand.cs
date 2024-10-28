using System;
using System.Collections.Generic;
using FSM.Editor.Actions;
using FSM.Editor.Nodes;
using JsonRawNode;
using NodeLib.Editor;
using NodeLib.Editor.Undo._Base;
using UnityEngine;

namespace FSM.Editor.Commands
{
    public class CloneNodeCommand : CommandBase
    {
        public const string ConstType = "CloneNodeCommand";

        private FsmEditorState _state;
        
        public CloneNodeCommand(FsmEditorState state, NodeLibEditor editor) : base(editor)
        {
            _state = state;
        }

        public CloneNodeCommand(NodeLibEditor editor, RawNode node) : base(editor, node)
        {
        }

        public override void Do()
        {
            var newState = new FsmEditorState(_state.Title, _state.Position + Vector2.one * 100)
            {
                Parallel = _state.Parallel,
                Editor = editor
            };

            foreach (var action in _state.Actions)
            {
                var dict = (Dictionary<string, object>)action.Serialize();
                dict.Remove("outputs");
                dict.Remove("portIds");
                
                var newAction = (FsmEditorActionBase)Activator.CreateInstance(action.GetType(), newState, new RawNode(dict));
                newState.Actions.Add(newAction);
            }
            
            editor.Nodes.Add(newState);
        }

        public override void Undo()
        {
            base.Undo();
            editor.Nodes.RemoveAt(editor.Nodes.Count - 1);
        }
    }
}