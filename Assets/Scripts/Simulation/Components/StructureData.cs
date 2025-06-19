using Unity.Entities;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Core;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Components
{
    public struct StructureData : IComponentData
    {
        public StructureType Type;
        public float3 Position;
        public float Health;
        public float Production;
        public float Maintenance;
        public float Influence;
        public float Stability;
        public float Prosperity;
        public float Poverty;
        public float Disease;
        public float Education;
        public float Knowledge;
        public float Wisdom;
        public bool IsActive;
        public FixedString128Bytes Owner;
    }
} 