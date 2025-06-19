using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct HistoricalEvent
    {
        public FixedString128Bytes Name;
        public FixedString512Bytes Description;
        public int Year;
        public EventType Type;
        public EventCategory Category;
        public float3 Location;
        public float Significance;
        public Entity SourceEntityId;
        public float Size;
    }
} 