using System.Collections.Generic;

namespace Praktikum6.BT
{
    public sealed class SequenceNode : BTNode
    {
        private readonly IReadOnlyList<BTNode> children;

        public SequenceNode(IReadOnlyList<BTNode> children)
        {
            this.children = children;
        }

        public override NodeState Tick()
        {
            foreach (BTNode child in children)
            {
                NodeState state = child.Tick();
                if (state != NodeState.Success) return state;
            }

            return NodeState.Success;
        }
    }
}
