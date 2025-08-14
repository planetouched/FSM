using System.Collections.Generic;
using FSM.Runtime.Actions._Base;
using FSM.Runtime.Debugging;
using FSM.Runtime.Factories;
using JsonRawNode;

namespace FSM.Runtime.Nodes
{
    public class FsmState
    {
        public static int GlobalUniqueId { get; private set; }

        public int UniqueId { get; private set; }
        private readonly bool _parallel;
        public List<FsmActionBase> Actions => _actions;
        
        private readonly List<FsmActionBase> _actions = new();
        private readonly string _finish;
        private readonly Fsm _fsm;
        private int _current;
        private FsmCompositeAction _compositeAction;
        private readonly RawNode _node;

        public string Id => _node.Key;

        public FsmState(RawNode node, Fsm fsm)
        {
            _fsm = fsm;
            _node = node;
            _parallel = node.GetBool("parallel");

            _finish = node.GetString("finish");

            int idx = 0;
            foreach (var actionNode in node.GetRawNodeArray("actions"))
            {
                var action = FsmFactory.Create(actionNode, fsm, this);
                _actions.Add(action);
                action.Idx = idx++;
            }
        }

        private void SequentialBehaviour()
        {
            if (_compositeAction != null)
            {
                _compositeAction.Finished -= OnActionFinish;
            }
            
            _compositeAction = new FsmCompositeAction();
            _compositeAction.SetParents(_fsm, this);
            _compositeAction.Finished += OnActionFinish;
            
            for (int i = _current; i < _actions.Count; i++)
            {
                _compositeAction.Add(_actions[i]);
                _current++;

                if (!_actions[i].Linked || i < _actions.Count - 1 && !_actions[i + 1].Linked)
                {
                    break;
                }
            }

            _compositeAction.Enter();
        }

        private void ParallelBehaviour()
        {
            _compositeAction = new FsmCompositeAction();
            _compositeAction.SetParents(_fsm, this);
            _compositeAction.Finished += OnActionFinish;
                
            foreach (var action in _actions)
            {
                _compositeAction.Add(action);
            }

            _compositeAction.Enter();
        }

        private void OnActionFinish()
        {
            if (_parallel)
            {
                foreach (var a in _actions)
                {
                    if (!a.IsFinished)
                    {
                        return;
                    }
                }

                ExitWithFinish();
            }
            else
            {
                if (_current == _actions.Count)
                {
                    ExitWithFinish();
                }
                else
                {
                    SequentialBehaviour();
                }
            }
        }

        private void ExitWithFinish()
        {
            if (string.IsNullOrEmpty(_finish)) return;
            
            if (_fsm.DebugMode)
            {
                FsmDebugEvent.Call(FsmDebugEvent.ExitWithEvent, (this, "Finish"));
            }

            _fsm.ToState(_finish);
        }

        public void Tick(float dt)
        {
            _compositeAction?.Tick(dt);
        }
        
        public void Enter()
        {
            if (_fsm.DebugMode)
            {
                FsmDebugEvent.Call(FsmDebugEvent.EnterState, Id);
            }

            if (_actions.Count == 0)
            {
                ExitWithFinish();
                return;
            }

            UniqueId = GlobalUniqueId++;

            if (_parallel)
            {
                ParallelBehaviour();
            }
            else
            {
                _current = 0;
                SequentialBehaviour();
            }
        }

        public void Exit()
        {
            if (_actions.Count == 0 || _compositeAction == null) return;

            if (_fsm.DebugMode)
            {
                FsmDebugEvent.Call(FsmDebugEvent.ExitState, Id);
            }

            _compositeAction.Finished -= OnActionFinish;
            _compositeAction.Finish();
            _compositeAction = null;
        }
    }
}