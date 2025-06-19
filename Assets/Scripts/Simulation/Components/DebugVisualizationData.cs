using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Components
{
    public struct DebugVisualizationData : IComponentData
    {
        public float3 Position;
        public float4 Color;
        public float Scale;
        public FixedString64Bytes Label;
    }
} 