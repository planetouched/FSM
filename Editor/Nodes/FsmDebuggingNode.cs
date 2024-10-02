using FSM.Editor.Ports;
using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Utils;
using UnityEngine;

namespace FSM.Editor.Nodes
{
    public abstract class FsmDebuggingNode : NodeBase
    {
        private float _flash;
        private const float FlashTime = 1.5f;

        private GUIStyle _debugStyle;
        public bool DebugActive { get; set; }

        protected FsmDebuggingNode(string title, Vector2 position) : base(title, position)
        {
            InitDebugStyle();
        }

        protected FsmDebuggingNode(RawNode data) : base(data)
        {
            InitDebugStyle();
        }

        private void InitDebugStyle()
        {
            _debugStyle = new GUIStyle();
            _debugStyle.normal.background = Texture2D.whiteTexture;
        }

        public void SetDebugMarker()
        {
            _flash = FlashTime;
        }

        protected override ConnectionPort ConnectionPortFactory(RawNode node)
        {
            return new DebuggingConnectionPort(this, node);
        }

        public override void Draw()
        {
            if (Application.isPlaying)
            {
                if (DebugActive)
                {
                    var color = new Color(0.8f, 0.2f, 0.8f, 1);
                    var rect = new Rect(Rect.position - Vector2.one * 4, Rect.size + Vector2.one * 8);
                    NodeLibUtil.DrawWithBackColor(color, () => GUI.Box(rect, string.Empty, _debugStyle));
                    GUI.changed = true;
                }
                
                if (_flash > 0)
                {
                    _flash -= Time.deltaTime;
                    if (_flash < 0)
                    {
                        _flash = 0;
                    }

                    var color = new Color(0.8f, 0.2f, 0.8f, _flash / FlashTime);
                    var rect = new Rect(Rect.position - Vector2.one * 4, Rect.size + Vector2.one * 8);
                    NodeLibUtil.DrawWithBackColor(color, () => GUI.Box(rect, string.Empty, _debugStyle));
                    GUI.changed = true;
                }
            }
            
            base.Draw();
        }
    }
}