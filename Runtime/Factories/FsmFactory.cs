using System;
using System.Collections.Generic;
using FSM.Runtime.Actions._Base;
using FSM.Runtime.Nodes;
using JsonRawNode;

namespace FSM.Runtime.Factories
{
    public static class FsmFactory
    {
        private static readonly Dictionary<string, Func<RawNode, FsmActionBase>> _factoryMethods = new();

        public static FsmActionBase Create(RawNode node, Fsm fsm, FsmState state)
        {
            var type = node.GetString("type");
            var action = _factoryMethods[type](node);
            action.SetParents(fsm, state);
            return action;
        }

        public static void Add(string type, Func<RawNode, FsmActionBase> factoryMethod)
        {
            _factoryMethods.Add(type, factoryMethod);
        }
    }
}