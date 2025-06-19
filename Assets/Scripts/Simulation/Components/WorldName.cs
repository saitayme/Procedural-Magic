using Unity.Entities;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Components
{
    public struct WorldName : IComponentData
    {
        public FixedString128Bytes Value;
    }
} 