using System;

namespace DeadDepths.AI
{
    public enum ZombieArchetype
    {
        Walker,
        Spitter,
        Charger
    }

    public enum ZombieState
    {
        IdlePatrol,
        Investigate,
        Chase,
        Attack,
        Search,
        Dead
    }
}
