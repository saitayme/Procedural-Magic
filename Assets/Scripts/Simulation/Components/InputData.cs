using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct InputData : IComponentData
    {
        public float2 MousePosition;
        public bool IsMouseButtonDown(int button) => false; // TODO: Implement actual input handling
    }
} 