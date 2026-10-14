using System.Collections.Generic;

namespace Praktikum6.BT
{
    public sealed class SelectorNode : BTNode
    {
        private readonly IReadOnlyList<BTNode> children;

        public SelectorNode(IReadOnlyList<BTNode> children)
        {
            this.children = children;
        }

        public override NodeState Tick()
        {
            foreach (BTNode child in children)
            {
                NodeState state = child.Tick();
                if (state != NodeState.Failure) return state;
            }

            return NodeState.Failure;
        }
    }
}
