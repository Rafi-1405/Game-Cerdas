using UnityEngine;

namespace Praktikum6.BT
{
    public sealed class CooldownDecorator : BTNode
    {
        private readonly BTNode child;
        private readonly float cooldown;
        private float nextAllowedTime;

        public CooldownDecorator(BTNode child, float cooldown)
        {
            this.child = child;
            this.cooldown = Mathf.Max(0f, cooldown);
        }

        public override NodeState Tick()
        {
            // Running keeps the Attack branch selected while the punch recharges.
            if (Time.time < nextAllowedTime) return NodeState.Running;

            NodeState state = child.Tick();
            if (state == NodeState.Success) nextAllowedTime = Time.time + cooldown;
            return state;
        }
    }
}
