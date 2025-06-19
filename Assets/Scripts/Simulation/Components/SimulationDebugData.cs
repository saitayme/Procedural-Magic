using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct SimulationDebugData : IComponentData
    {
        public int CurrentYear;
        public float TimeScale;
        public float DeltaTime;
        public NativeArray<CivilizationData> Civilizations;
        public NativeArray<HistoricalEventData> RecentEvents;
    }
} 