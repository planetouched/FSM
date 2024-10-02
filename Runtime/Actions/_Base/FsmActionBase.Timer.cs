using System;
using System.Collections.Generic;

namespace FSM.Runtime.Actions._Base
{
    public partial class FsmActionBase
    {
        private readonly List<Timer> _timers = new();
        private readonly List<Timer> _removeTimers = new();
        private readonly List<Timer> _addTimers = new();

        protected void SetTimer(string name, Action<Timer> call, float duration, bool once)
        {
            if (!fsm.IsActiveState(state)) return;
            
            _addTimers.Add(new Timer(name, call, duration, once));
        }

        private void UpdateTimers(float dt)
        {
            if (_addTimers.Count > 0)
            {
                _timers.AddRange(_addTimers);
                _addTimers.Clear();
            }
            
            foreach (var timer in _timers)
            {
                timer.Tick(dt);
                
                if (timer.IsFinished)
                {
                    _removeTimers.Add(timer);
                }
            }

            if (_removeTimers.Count > 0)
            {
                foreach (var timer in _removeTimers)
                {
                    _timers.Remove(timer);
                }
                
                _removeTimers.Clear();
            }
        }
        
        protected class Timer
        {
            private readonly float _duration;
            private readonly bool _once;
            public string Name { get; }

            private float _elapsed;
            private readonly Action<Timer> _call;

            public bool IsFinished { get; private set; }
            
            public Timer(string name, Action<Timer> call, float duration, bool once)
            {
                Name = name;
                _call = call;
                _duration = duration;
                _once = once;
            }

            public void Tick(float dt)
            {
                if (IsFinished) return;
                
                _elapsed += dt;
                if (_elapsed >= _duration)
                {
                    _elapsed -= _duration;
                    _call(this);
                    
                    IsFinished = _once;
                }
            }
        }
    }
}