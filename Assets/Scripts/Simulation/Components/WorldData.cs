using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Components
{
    public struct WorldData : IComponentData
    {
        public float3 WorldSize;
        public float3 WorldCenter;
        public float WorldScale;
        public int Seed;
        public float TimeScale;
        public int CurrentYear;
        public int StartYear;
        public int EndYear;
    }
} 