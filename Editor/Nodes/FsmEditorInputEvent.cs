using FSM.Editor.Ports;
using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Serialization;
using UnityEngine;

namespace FSM.Editor.Nodes
{
    public class FsmEditorInputEvent : FsmDebuggingNode
    {
        public const string Type = "input";
        protected override string NodeType => Type;
        
        public FsmEditorInputEvent(string title, Vector2 position) : base(title, position)
        {
            HeaderColor = new Color32(200, 70, 70, 255);
            
            Ports.Add(new DebuggingConnectionPort(
                this, 
                "Out", 
                new Vector2(0, 16), 
                PortSide.Right, 
                PortIcon.Right, 
                PortDirection.Out, 
                true, 
                Color.green, 
                "state"));
        }

        public FsmEditorInputEvent(RawNode data) : base(data)
        {
        }

        protected override void OnInitialize(RawNode rawData)
        {
        }
        
        public object SerializeRuntime()
        {
            var conn = Editor.FindConnection(Ports[0]);
            if (conn != null)
            {
                return SerializeUtil.Dict().SetArgs("to", conn.InPort.ParentNode.Id, "title", Title);
            }

            return null;
        }
    }
}