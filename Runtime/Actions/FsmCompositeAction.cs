using System;
using System.Collections.Generic;
using FSM.Runtime.Nodes;

namespace FSM.Runtime.Actions._Base
{
    sealed class FsmCompositeAction
    {
        private readonly List<FsmActionBase> _actions = new();
        public event Action Finished;

        private Fsm _fsm;
        private FsmState _state;

        public void Add(FsmActionBase action)
        {
            _actions.Add(action);
        }

        public void SetParents(Fsm fsm, FsmState state)
        {
            _fsm = fsm;
            _state = state;
        }

        private void SubscribeAll()
        {
            foreach (var action in _actions)
            {
                action.Finished += OnActionFinish;
            }
        }

        private void UnsubscribeAll()
        {
            foreach (var action in _actions)
            {
                action.Finished -= OnActionFinish;
            }
        }

        private void OnActionFinish(FsmActionBase action)
        {
            foreach (var a in _actions)
            {
                if (!a.IsFinished) return;
            }

            Finish();
        }

        public void Enter()
        {
            SubscribeAll();

            foreach (var action in _actions)
            {
                action.ClearFlags();
            }

            foreach (var action in _actions)
            {
                action.Enter();

                if (_state.UniqueId != _fsm.Current.UniqueId)
                {
                    break;
                }
            }
        }

        public void Tick(float dt)
        {
            foreach (var action in _actions)
            {
                if (action.IsWorking)
                {
                    action.Tick(dt);
                }
                
                if (_state.UniqueId != _fsm.Current.UniqueId)
                {
                    break;
                }
            }
        }

        public void Finish()
        {
            UnsubscribeAll();

            foreach (var action in _actions)
            {
                if (action.IsWorking)
                {
                    action.Finish();
                }
            }

            Finished?.Invoke();
        }
    }
}