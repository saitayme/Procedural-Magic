using Unity.Entities;
using Unity.Collections;
using Unity.Burst;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct EntityReference : IComponentData
    {
        public Entity Entity;
        public FixedString64Bytes Name;
        public FixedString128Bytes Description;
    }
} 