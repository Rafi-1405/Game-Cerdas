using System;

namespace Praktikum6.BT
{
    public sealed class ActionNode : BTNode
    {
        private readonly Func<NodeState> action;

        public ActionNode(Func<NodeState> action)
        {
            this.action = action;
        }

        public override NodeState Tick()
        {
            return action();
        }
    }
}
