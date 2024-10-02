using System.Collections.Generic;
using FSM.Editor.Actions;
//using FSM.Editor.Attributes;
using FSM.Editor.Nodes;
using JsonRawNode;

namespace FSM.Editor.Test
{
    //[FsmEditorAction("Category", "test", "TestAction")]
    public class FsmEditorTestAction : FsmEditorActionBase
    {
        protected override Dictionary<string, object> OutputEvents { get; } = new() { { "Iv1", false }, { "Iv2", false }, { "Iv3", false } };

        public FsmEditorTestAction(FsmEditorState parent, RawNode node = null) : base(parent, node)
        {
        }
    }
}