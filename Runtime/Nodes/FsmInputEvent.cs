using JsonRawNode;

namespace FSM.Runtime.Nodes
{
    public class FsmInputEvent
    {
        public string Name { get; }
        public string To { get; }
        
        public FsmInputEvent(RawNode node)
        {
            Name = node.GetString("title");
            To = node.GetString("to");
        }
    }
}