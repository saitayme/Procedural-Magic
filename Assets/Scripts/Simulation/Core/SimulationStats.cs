using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

namespace ProceduralWorld.Simulation.Core
{
    [BurstCompile]
    public struct SimulationStats : IComponentData
    {
        public float SimulationTime;
        public int CivilizationCount;
        public int ResourceCount;
        public int StructureCount;
        public int EventCount;
        public float AverageFPS;
        public float MemoryUsage;
        public int ActiveSystems;
        public int TotalEntities;
    }
} 