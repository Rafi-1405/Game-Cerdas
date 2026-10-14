using System;

namespace Praktikum6.BT
{
    public sealed class ConditionNode : BTNode
    {
        private readonly Func<bool> condition;

        public ConditionNode(Func<bool> condition)
        {
            this.condition = condition;
        }

        public override NodeState Tick()
        {
            return condition() ? NodeState.Success : NodeState.Failure;
        }
    }
}
