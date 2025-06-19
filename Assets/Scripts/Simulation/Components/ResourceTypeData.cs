using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct ResourceTypeData : IComponentData
    {
        public Core.ResourceType Type;
        public float BaseValue;
        public float CurrentValue;
        public float MaxValue;
        public float MinValue;
        public float GrowthRate;
        public float DecayRate;
        public float Demand;
        public float Supply;
        public float Price;
    }
} 