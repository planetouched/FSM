using System;
using System.Collections.Generic;
using FSM.Runtime.Debugging;
using FSM.Runtime.Nodes;
using JsonRawNode;

namespace FSM.Runtime
{
    public class Fsm
    {
        public event Action<Fsm> Dropped;
        public event Action<string, bool, Fsm> ExternalEventCalled;
        
        public FsmState Current { get; private set; }

        public Dictionary<string, FsmState> States => _states;
        
        private readonly Dictionary<string, FsmInputEvent> _inputs = new();
        private readonly Dictionary<string, FsmState> _states = new();
        private readonly Dictionary<string, object> _objects = new();

        public string CurrentInputEventName { get; private set; }
        
        public bool DebugMode { get; private set; }

        public Fsm(RawNode node)
        {
            if (node.GetString("type") != "fsm-runtime")
            {
                throw new Exception("FSM structure's error");
            }
            
            foreach (var inputNode in node.GetNode("inputs").GetSortedCollection())
            {
                _inputs.Add(inputNode.Key, new FsmInputEvent(inputNode));
            }
            
            foreach (var stateNode in node.GetNode("states").GetSortedCollection())
            {
                _states.Add(stateNode.Key, new FsmState(stateNode, this));
            }
        }
        
        public void ToState(string guid)
        {
            var newState = _states[guid];
            Current?.Exit();
            Current = newState;
            Current.Enter();
        }

        public void SetDebugMode(bool debug)
        {
            if (DebugMode == debug) return;
            
            DebugMode = debug;
            
            if (debug)
            {
                FsmDebugEvent.Call(FsmDebugEvent.EnterState, Current.Id);
            }
        }

        public bool IsActiveState(FsmState state)
        {
            return Current.UniqueId == state.UniqueId;
        }

        public void CallRootComponent(string inputEventName, bool copyFsmValues = false)
        {
            ExternalEventCalled?.Invoke(inputEventName, copyFsmValues, this);
        }
        
        public bool StartWithEvent(string inputEventName, Fsm copyValuesFrom = null)
        {
            foreach (var pair in _inputs)
            {
                if (pair.Value.Name == inputEventName)
                {
                    if (copyValuesFrom != null)
                    {
                        foreach (var obj in copyValuesFrom._objects)
                        {
                            BindObject(obj.Key, obj.Value);
                        }
                    }

                    CurrentInputEventName = inputEventName;
                    
                    if (!string.IsNullOrEmpty(pair.Value.To))
                    {
                        ToState(pair.Value.To);
                        return true;
                    }
                    
                    break;
                }
            }

            return false;
        }

        public void Drop()
        {
            Cancel();

            Dropped?.Invoke(this);
            Dropped = null;
        }

        public void Tick(float dt)
        {
            Current?.Tick(dt);
        }
        
        public void Cancel()
        {
            Current?.Exit();
        }
        
        public void BindObject(string key, object obj)
        {
            _objects[key] = obj;
        }
        
        public void UnbindObject(string key)
        {
            _objects.Remove(key);
        }
        
        public void UnbindAllObjects()
        {
            _objects.Clear();
        }

        public bool ContainsKey(string key)
        {
            return _objects.ContainsKey(key);
        }
        
        public T GetObject<T>(string key)
        {
            return (T) _objects[key];
        }
    }
}