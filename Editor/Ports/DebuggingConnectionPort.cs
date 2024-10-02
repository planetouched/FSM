using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Nodes;
using UnityEngine;

namespace FSM.Editor.Ports
{
    public class DebuggingConnectionPort : ConnectionPort
    {
        private float _flash;
        private const float FlashTime = 1.5f;
        
        public DebuggingConnectionPort(NodeBase parent, string name, Vector2 sideOffset, PortSide portSide, PortIcon icon, PortDirection portDirection, bool multipleConnections, Color32 color, string portType) : base(parent, name, sideOffset, portSide, icon, portDirection, multipleConnections, color, portType)
        {
        }

        public DebuggingConnectionPort(NodeBase parent, RawNode node): base(parent, node)
        {
        }

        public void SetDebugMarker()
        {
            _flash = FlashTime;
        }
        
        public override void Draw()
        {
            base.Draw();

            if (Application.isPlaying)
            {
                if (_flash > 0)
                {
                    _flash -= Time.deltaTime;
                    if (_flash < 0)
                    {
                        _flash = 0;
                    }
                    
                    var color = new Color(0.8f, 0.2f, 0.8f, _flash / FlashTime);

                    var bc = GUI.color;
                    GUI.color = color;
                    GUI.DrawTexture(Rect, IconTexture);
                    GUI.color = bc;
                    GUI.changed = true;
                }
            }
        }
    }
}