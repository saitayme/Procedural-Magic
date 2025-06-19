using Unity.Entities;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Components
{
    public struct EntityMarker : IComponentData
    {
        public FixedString128Bytes Label;
    }
} 