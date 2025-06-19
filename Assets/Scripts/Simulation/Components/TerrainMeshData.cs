using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct TerrainMeshData : IComponentData
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> Vertices;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> Triangles;
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> Normals;
        [NativeDisableParallelForRestriction]
        public NativeArray<float4> Colors;
        [NativeDisableParallelForRestriction]
        public NativeArray<float2> UVs;

        public void Initialize(int vertexCount, int triangleCount)
        {
            Vertices = new NativeArray<float3>(vertexCount, Allocator.Persistent);
            Triangles = new NativeArray<int>(triangleCount, Allocator.Persistent);
            Normals = new NativeArray<float3>(vertexCount, Allocator.Persistent);
            Colors = new NativeArray<float4>(vertexCount, Allocator.Persistent);
            UVs = new NativeArray<float2>(vertexCount, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (Vertices.IsCreated) Vertices.Dispose();
            if (Triangles.IsCreated) Triangles.Dispose();
            if (Normals.IsCreated) Normals.Dispose();
            if (Colors.IsCreated) Colors.Dispose();
            if (UVs.IsCreated) UVs.Dispose();
        }
    }
} 