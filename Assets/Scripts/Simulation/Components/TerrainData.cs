using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct TerrainData : IComponentData
    {
        public int Resolution;
        public float HeightScale;
        public float WorldSize;
        public float NoiseScale;
        public float Persistence;
        public float Lacunarity;
        public int Octaves;
        public float2 Offset;
        public int Seed;

        [NativeDisableParallelForRestriction]
        public NativeArray<float> HeightMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> TemperatureMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> MoistureMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<Core.BiomeType> BiomeMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> ResourceMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<Core.ResourceType> ResourceTypeMap;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int2, bool> GeneratedChunks;

        public float Height;
        public float Moisture;
        public float Temperature;
        public Core.BiomeType BiomeType;
        public float3 Position;
        public float Scale;
        public float GrowthRate;
        public float MaxHeight;
        public float MaxMoisture;
        public float MaxTemperature;
        public float ResourceAmount;
        public float ResourceValue;
        public Core.ResourceType ResourceType;
        public bool IsDirty;
        public float LastUpdateTime;
    }
} 