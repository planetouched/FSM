namespace FSM.Runtime.Debugging
{
    public interface IFsmDebugMode
    {
        string StateGuid { get; }
        void EnableDebugging();
        void DisableDebugging();
    }
}