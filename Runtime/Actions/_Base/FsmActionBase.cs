using System;
using System.Collections.Generic;
using FSM.Runtime.Debugging;
using FSM.Runtime.Nodes;
using JsonRawNode;

namespace FSM.Runtime.Actions._Base
{
    public abstract partial class FsmActionBase
    {
        protected Fsm fsm;
        protected readonly RawNode node;
        protected FsmState state;

        public event Action<FsmActionBase> Finished;
        public bool IsFinished { get; private set; }
        public bool IsWorking { get; private set; }
        public bool Linked { get; }
        public bool Cyclical { get; }
        public int Idx { get; set; }

        private readonly Dictionary<string, string> _to = new();

        protected FsmActionBase(RawNode node)
        {
            foreach (var toNode in node.GetNode("to").GetSortedCollection())
            {
                _to.Add(toNode.Key, toNode.ToString());
            }

            Linked = node.GetBool("linked");
            Cyclical = node.GetBool("cc");
            this.node = node;
        }

        public void SetParents(Fsm fsm, FsmState state)
        {
            this.fsm = fsm;
            this.state = state;
        }

        protected abstract void OnEnter();
        protected abstract void OnFinish();

        public void ClearFlags()
        {
            IsWorking = false;
            IsFinished = false;
        }

        public bool HasExitWith(string outputEventName)
        {
            return _to.ContainsKey(outputEventName);
        }
        
        protected bool ExitWithEvent(string outputEventName)
        {
            if (!fsm.IsActiveState(state)) return false;
            if (!_to.ContainsKey(outputEventName)) return false;

            if (fsm.DebugMode)
            {
                FsmDebugEvent.Call(FsmDebugEvent.ExitWithEvent, (state, outputEventName));
            }

            fsm.ToState(_to[outputEventName]);
            return true;
        }

        public void Enter()
        {
            if (IsWorking) return;
            IsWorking = true;

            if (fsm.DebugMode)
            {
                FsmDebugEvent.Call(FsmDebugEvent.EnterAction, Idx);
            }

            OnEnter();
        }

        public void Tick(float dt)
        {
            UpdateTimers(dt);
            
            if (state.UniqueId != fsm.Current.UniqueId)
            {
                return;
            }
            
            OnTick();
        }

        protected virtual void OnTick()
        {
        }

        public void Finish()
        {
            if (IsFinished) return;
            IsFinished = true;
            IsWorking = false;

            if (fsm.DebugMode)
            {
                FsmDebugEvent.Call(FsmDebugEvent.ExitAction, Idx);
            }

            OnFinish();
            Finished?.Invoke(this);
        }
    }
}