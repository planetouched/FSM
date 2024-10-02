using System;

namespace FSM.Editor.Attributes
{
    public class FsmEditorActionAttribute : Attribute
    {
        public string Category { get; }
        public string Name { get; }
        public string Type { get; }
        public Type ActionType { get; set; }
        
        public FsmEditorActionAttribute(string category, string type, string name)
        {
            Category = category;
            Type = type;
            Name = name;
        }
    }
}