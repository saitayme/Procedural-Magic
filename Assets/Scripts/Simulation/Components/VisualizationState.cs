using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct VisualizationState : IComponentData
    {
        public bool IsDirty;
        public float LastUpdateTime;
        public JobHandle JobHandle;
    }
} 