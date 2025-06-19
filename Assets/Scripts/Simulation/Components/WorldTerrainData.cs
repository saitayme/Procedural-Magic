using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct WorldTerrainData : IComponentData
    {
        // Terrain dimensions
        public int Resolution;
        public float HeightScale;
        public float WorldSize;
        public int Seed;

        // Terrain data
        [NativeDisableParallelForRestriction]
        public NativeArray<float> HeightMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> TemperatureMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> MoistureMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<ProceduralWorld.Simulation.Core.BiomeType> BiomeMap;

        // Erosion data
        [NativeDisableParallelForRestriction]
        public NativeArray<float> ErosionMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> SedimentMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> WaterMap;

        // Resource data
        [NativeDisableParallelForRestriction]
        public NativeArray<float> ResourceMap;
        [NativeDisableParallelForRestriction]
        public NativeArray<ProceduralWorld.Simulation.Core.ResourceType> ResourceTypeMap;

        // Climate data
        public float GlobalTemperature;
        public float GlobalMoisture;
        public float ClimateChangeRate;

        // Generation parameters
        public float NoiseScale;
        public float Persistence;
        public float Lacunarity;
        public int Octaves;
        public float2 Offset;

        // Terrain features
        public bool HasMountain;
        public bool HasRiver;
        public bool HasForest;
        public bool HasDesert;
        public bool HasOcean;
        public ProceduralWorld.Simulation.Core.BiomeType Biome;

        public NativeHashSet<int2> GeneratedChunks;

        public static WorldTerrainData Default => new WorldTerrainData
        {
            Resolution = 256,
            HeightScale = 100f,
            WorldSize = 1000f,
            NoiseScale = 50f,
            Persistence = 0.5f,
            Lacunarity = 2f,
            Octaves = 6,
            Offset = float2.zero,
            GlobalTemperature = 15f,
            GlobalMoisture = 0.5f,
            ClimateChangeRate = 0.001f,
            HasMountain = false,
            HasRiver = false,
            HasForest = false,
            HasDesert = false,
            HasOcean = false,
            Biome = ProceduralWorld.Simulation.Core.BiomeType.None
        };
    }

    public struct TerrainChunkData : IComponentData
    {
        public int2 ChunkCoord;
        public int ChunkSize;
        public NativeArray<float> HeightData;
        public NativeArray<float> TemperatureData;
        public NativeArray<float> MoistureData;
        public NativeArray<ProceduralWorld.Simulation.Core.BiomeType> BiomeData;
        public NativeArray<ProceduralWorld.Simulation.Core.ResourceType> ResourceData;
    }
} 