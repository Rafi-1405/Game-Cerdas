namespace Praktikum6.BT
{
    public enum NodeState
    {
        Success,
        Failure,
        Running
    }

    public abstract class BTNode
    {
        public abstract NodeState Tick();
    }
}
