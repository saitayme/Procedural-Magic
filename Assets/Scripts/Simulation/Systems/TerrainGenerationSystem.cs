using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using CoreBiomeType = ProceduralWorld.Simulation.Core.BiomeType;
using CoreResourceType = ProceduralWorld.Simulation.Core.ResourceType;
using CoreVisualizationState = ProceduralWorld.Simulation.Core.VisualizationState;
using System;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    [UpdateBefore(typeof(CivilizationSpawnSystem))]
    public partial struct TerrainGenerationSystem : ISystem
    {
        private bool _isInitialized;
        private EntityQuery _configQuery;
        private EntityQuery _visualizationQuery;
        private EntityQuery _terrainQuery;

        public void OnCreate(ref SystemState state)
        {
            Debug.Log("[TerrainGenerationSystem] System created");
            state.RequireForUpdate<SimulationConfig>();
            state.RequireForUpdate<CoreVisualizationState>();
            state.RequireForUpdate<WorldTerrainData>();
            
            _configQuery = state.GetEntityQuery(ComponentType.ReadOnly<SimulationConfig>());
            _visualizationQuery = state.GetEntityQuery(ComponentType.ReadWrite<CoreVisualizationState>());
            _terrainQuery = state.GetEntityQuery(ComponentType.ReadWrite<WorldTerrainData>());
        }

        public void OnDestroy(ref SystemState state)
        {
            // Cleanup if needed
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_isInitialized)
                return;

            Debug.Log("[TerrainGenerationSystem] Initializing terrain data");

            // Get config and visualization state
            var config = state.EntityManager.GetComponentData<SimulationConfig>(_configQuery.GetSingletonEntity());
            var visualizationState = state.EntityManager.GetComponentData<CoreVisualizationState>(_visualizationQuery.GetSingletonEntity());
            var terrainEntity = _terrainQuery.GetSingletonEntity();
            var terrainData = state.EntityManager.GetComponentData<WorldTerrainData>(terrainEntity);

            // Calculate capacity for generated chunks
            var chunkCapacity = config.TerrainResolution * config.TerrainResolution / 256;

            // Initialize terrain data arrays if they don't exist
            if (!terrainData.HeightMap.IsCreated)
            {
                terrainData.HeightMap = new NativeArray<float>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
                terrainData.TemperatureMap = new NativeArray<float>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
                terrainData.MoistureMap = new NativeArray<float>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
                terrainData.BiomeMap = new NativeArray<CoreBiomeType>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
                terrainData.ResourceMap = new NativeArray<float>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
                terrainData.ResourceTypeMap = new NativeArray<CoreResourceType>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
                terrainData.GeneratedChunks = new NativeHashSet<int2>(chunkCapacity, Allocator.Persistent);
                terrainData.WaterMap = new NativeArray<float>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
                terrainData.ErosionMap = new NativeArray<float>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
                terrainData.SedimentMap = new NativeArray<float>(config.TerrainResolution * config.TerrainResolution, Allocator.Persistent);
            }

            // Update terrain data properties
            terrainData.Resolution = config.TerrainResolution;
            terrainData.WorldSize = config.WorldSize;
            terrainData.HeightScale = config.HeightScale;
            terrainData.NoiseScale = config.NoiseScale;
            terrainData.Seed = config.Seed;
            terrainData.Persistence = 0.5f;
            terrainData.Lacunarity = 2f;
            terrainData.Octaves = 6;
            terrainData.Offset = float2.zero;
            terrainData.GlobalTemperature = 15f;
            terrainData.GlobalMoisture = 0.5f;
            terrainData.ClimateChangeRate = 0.001f;

            // First pass: Generate terrain data in parallel
            var generateJob = new GenerateTerrainJob
            {
                HeightMap = terrainData.HeightMap,
                TemperatureMap = terrainData.TemperatureMap,
                MoistureMap = terrainData.MoistureMap,
                BiomeMap = terrainData.BiomeMap,
                ResourceMap = terrainData.ResourceMap,
                ResourceTypeMap = terrainData.ResourceTypeMap,
                WaterMap = terrainData.WaterMap,
                Resolution = config.TerrainResolution,
                WorldSize = config.WorldSize,
                HeightScale = config.HeightScale,
                NoiseScale = config.NoiseScale,
                Seed = config.Seed
            };

            state.Dependency = generateJob.Schedule(config.TerrainResolution * config.TerrainResolution, 64, state.Dependency);
            state.Dependency.Complete();

            // Second pass: Collect generated chunks
            var collectChunksJob = new CollectChunksJob
            {
                Resolution = config.TerrainResolution,
                GeneratedChunks = terrainData.GeneratedChunks
            };

            state.Dependency = collectChunksJob.Schedule(state.Dependency);
            state.Dependency.Complete();

            // Update the terrain data component
            state.EntityManager.SetComponentData(terrainEntity, terrainData);

            Debug.Log("[TerrainGenerationSystem] Terrain data initialized");

            // Mark visualization as dirty
            visualizationState.IsDirty = true;
            state.EntityManager.SetComponentData(_visualizationQuery.GetSingletonEntity(), visualizationState);

            // Debug output for terrain analysis
            float minHeight = float.MaxValue, maxHeight = float.MinValue;
            var biomeCounts = new System.Collections.Generic.Dictionary<CoreBiomeType, int>();
            for (int i = 0; i < terrainData.HeightMap.Length; i++) {
                float h = terrainData.HeightMap[i];
                if (h < minHeight) minHeight = h;
                if (h > maxHeight) maxHeight = h;
                var biome = terrainData.BiomeMap[i];
                if (!biomeCounts.ContainsKey(biome)) biomeCounts[biome] = 0;
                biomeCounts[biome]++;
            }
            Debug.Log($"[TerrainGenerationSystem] HeightMap min: {minHeight}, max: {maxHeight}");
            
            // Add mountain ranges post-processing for more realistic mountain placement
            AddMountainRanges(terrainData);
            
            // Final biome count after mountain generation
            biomeCounts.Clear();
            for (int i = 0; i < terrainData.BiomeMap.Length; i++) {
                var biome = terrainData.BiomeMap[i];
                if (!biomeCounts.ContainsKey(biome)) biomeCounts[biome] = 0;
                biomeCounts[biome]++;
            }
            foreach (var kvp in biomeCounts) Debug.Log($"[BiomeDistribution] {kvp.Key}: {kvp.Value}");

            _isInitialized = true;
        }

        private void AddMountainRanges(WorldTerrainData terrainData)
        {
            int resolution = terrainData.Resolution;
            var random = new Unity.Mathematics.Random((uint)terrainData.Seed + 12345);
            
            // Generate 2-4 mountain ranges
            int numRanges = random.NextInt(2, 5);
            
            for (int range = 0; range < numRanges; range++)
            {
                // Random starting point for mountain range
                float2 start = new float2(random.NextFloat(0.2f, 0.8f), random.NextFloat(0.2f, 0.8f));
                float2 direction = math.normalize(new float2(random.NextFloat(-1f, 1f), random.NextFloat(-1f, 1f)));
                float length = random.NextFloat(0.3f, 0.6f); // Length as fraction of map
                
                // Create mountain range
                int numSegments = (int)(length * resolution * 0.5f);
                for (int seg = 0; seg < numSegments; seg++)
                {
                    float t = (float)seg / numSegments;
                    float2 pos = start + direction * length * t;
                    
                    // Add some curvature to the mountain range
                    float curve = noise.snoise(new float2(t * 3f, range * 100f)) * 0.1f;
                    float2 perpendicular = new float2(-direction.y, direction.x);
                    pos += perpendicular * curve;
                    
                    // Ensure position is within bounds
                    pos = math.clamp(pos, 0.05f, 0.95f);
                    
                    // Convert to grid coordinates
                    int centerX = (int)(pos.x * resolution);
                    int centerY = (int)(pos.y * resolution);
                    
                    // Mountain range width varies along its length
                    float widthVariation = 1f - math.abs(t - 0.5f) * 2f; // Wider in middle
                    int width = (int)(random.NextFloat(8f, 20f) * widthVariation);
                    
                    // Apply mountain elevation in a circular area
                    for (int dy = -width; dy <= width; dy++)
                    {
                        for (int dx = -width; dx <= width; dx++)
                        {
                            int x = centerX + dx;
                            int y = centerY + dy;
                            
                            if (x < 0 || y < 0 || x >= resolution || y >= resolution) continue;
                            
                            int index = y * resolution + x;
                            
                            // Skip if already ocean or coast
                            if (terrainData.BiomeMap[index] == CoreBiomeType.Ocean || 
                                terrainData.BiomeMap[index] == CoreBiomeType.Coast) continue;
                            
                            float distance = math.length(new float2(dx, dy));
                            if (distance <= width)
                            {
                                // Mountain height falloff
                                float falloff = 1f - (distance / width);
                                falloff = falloff * falloff; // Smooth falloff
                                
                                // Increase height for mountain
                                float mountainHeight = 0.7f + falloff * 0.25f;
                                if (terrainData.HeightMap[index] < mountainHeight)
                                {
                                    terrainData.HeightMap[index] = mountainHeight;
                                    
                                    // Assign mountain biome if high enough
                                    if (mountainHeight > 0.75f)
                                    {
                                        terrainData.BiomeMap[index] = CoreBiomeType.Mountains;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct GenerateTerrainJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<float> HeightMap;
            [WriteOnly] public NativeArray<float> TemperatureMap;
            [WriteOnly] public NativeArray<float> MoistureMap;
            [WriteOnly] public NativeArray<CoreBiomeType> BiomeMap;
            [WriteOnly] public NativeArray<float> ResourceMap;
            [WriteOnly] public NativeArray<CoreResourceType> ResourceTypeMap;
            [WriteOnly] public NativeArray<float> WaterMap;
            public int Resolution;
            public float WorldSize;
            public float HeightScale;
            public float NoiseScale;
            public int Seed;

            public void Execute(int index)
            {
                int x = index % Resolution;
                int y = index / Resolution;
                float2 pos = new float2(x, y);
                float2 normalizedPos = pos / Resolution;

                // Step 1: Generate base climate data locally (don't read from WriteOnly arrays)
                float temperature = GenerateTemperature(x, y, normalizedPos);
                float moisture = GenerateMoisture(x, y, normalizedPos);
                
                // Store climate data in arrays
                TemperatureMap[index] = temperature;
                MoistureMap[index] = moisture;
                
                // Step 2: Determine biome based on climate and geography
                CoreBiomeType biome = DetermineBiome(temperature, moisture, normalizedPos);
                BiomeMap[index] = biome;
                
                // Step 3: Generate height based on biome type (realistic approach)
                float height = GenerateRealisticHeight(x, y, normalizedPos, biome);
                HeightMap[index] = height;

                // Step 4: Generate water features
                GenerateWaterFeatures(index, x, y, normalizedPos, biome, height);

                // Step 5: Generate resources based on biome and terrain
                GenerateResources(index, normalizedPos, biome, height);
            }

            private float GenerateTemperature(int x, int y, float2 normalizedPos)
            {
                // Temperature: affected by latitude (distance from equator) and elevation
                float latitude = math.abs(normalizedPos.y - 0.5f) * 2f; // 0 at equator, 1 at poles
                float latitudeTemp = 1f - latitude * 0.7f; // Warmer at equator
                
                // Add some noise for climate variation
                float tempNoise = noise.snoise(new float2(x, y) * NoiseScale * 0.3f + Seed);
                tempNoise = (tempNoise + 1) * 0.5f;
                
                return math.clamp(latitudeTemp + (tempNoise - 0.5f) * 0.3f, 0f, 1f);
            }

            private float GenerateMoisture(int x, int y, float2 normalizedPos)
            {
                // Moisture: affected by distance from water bodies and prevailing winds
                float moistureNoise1 = noise.snoise(new float2(x, y) * NoiseScale * 0.4f + Seed + 1000);
                float moistureNoise2 = noise.snoise(new float2(x, y) * NoiseScale * 0.8f + Seed + 2000);
                
                // Combine different scales of moisture patterns
                float moisture = (moistureNoise1 + moistureNoise2 * 0.5f) / 1.5f;
                moisture = (moisture + 1) * 0.5f;
                
                // Add coastal moisture effect (will be refined later)
                float distanceFromEdge = math.min(
                    math.min(normalizedPos.x, 1f - normalizedPos.x),
                    math.min(normalizedPos.y, 1f - normalizedPos.y)
                );
                float coastalMoisture = math.saturate(1f - distanceFromEdge * 3f);
                moisture = math.saturate(moisture + coastalMoisture * 0.3f);
                
                return moisture;
            }

            private void GenerateClimate(int index, int x, int y, float2 normalizedPos)
            {
                // This method is now obsolete - keeping for compatibility
                float temperature = GenerateTemperature(x, y, normalizedPos);
                float moisture = GenerateMoisture(x, y, normalizedPos);
                
                TemperatureMap[index] = temperature;
                MoistureMap[index] = moisture;
            }

            private CoreBiomeType DetermineBiome(float temperature, float moisture, float2 normalizedPos)
            {
                // First, determine if this should be ocean based on continental generation
                float continentNoise = noise.snoise(normalizedPos * 2f + Seed + 5000);
                continentNoise = (continentNoise + 1) * 0.5f;
                
                // Create island/continent patterns
                float distanceFromCenter = math.length(normalizedPos - 0.5f) * 2f;
                float continentMask = math.saturate(1f - distanceFromCenter * 1.2f + continentNoise * 0.4f);
                
                // Ocean threshold - areas far from continents become ocean
                if (continentMask < 0.3f)
                {
                    return CoreBiomeType.Ocean;
                }
                
                // Coast areas - near ocean but on land
                if (continentMask < 0.45f)
                {
                    return CoreBiomeType.Coast;
                }

                // Check for mountain ranges using ridge noise
                float mountainNoise1 = noise.snoise(normalizedPos * 4f + Seed + 6000);
                float mountainNoise2 = noise.snoise(normalizedPos * 8f + Seed + 7000);
                
                // Create mountain ridges
                float ridgeNoise = math.abs(mountainNoise1) + math.abs(mountainNoise2) * 0.5f;
                ridgeNoise = 1f - ridgeNoise; // Invert so ridges are high values
                
                // Mountain threshold - create mountain ranges
                if (ridgeNoise > 0.7f && continentMask > 0.6f)
                {
                    return CoreBiomeType.Mountains;
                }

                // Land biomes based on temperature and moisture
                if (temperature < 0.2f)
                {
                    return CoreBiomeType.Tundra;
                }
                else if (temperature < 0.4f)
                {
                    if (moisture > 0.6f) return CoreBiomeType.Swamp;
                    if (moisture > 0.4f) return CoreBiomeType.Forest;
                    return CoreBiomeType.Plains;
                }
                else if (temperature < 0.7f)
                {
                    if (moisture > 0.7f) return CoreBiomeType.Rainforest;
                    if (moisture > 0.5f) return CoreBiomeType.Forest;
                    if (moisture > 0.3f) return CoreBiomeType.Plains;
                    return CoreBiomeType.Desert;
                }
                else // Hot climates
                {
                    if (moisture > 0.8f) return CoreBiomeType.Rainforest;
                    if (moisture > 0.4f) return CoreBiomeType.Forest;
                    if (moisture > 0.2f) return CoreBiomeType.Plains;
                    return CoreBiomeType.Desert;
                }
            }

            private float GenerateRealisticHeight(int x, int y, float2 normalizedPos, CoreBiomeType biome)
            {
                // Base height determined by biome type
                float baseHeight = GetBiomeBaseHeight(biome);
                
                // Add appropriate noise based on biome
                float heightVariation = GetBiomeHeightVariation(x, y, normalizedPos, biome);
                
                // Combine base height with variation
                float finalHeight = math.saturate(baseHeight + heightVariation);
                
                return finalHeight;
            }

            private float GetBiomeBaseHeight(CoreBiomeType biome)
            {
                switch (biome)
                {
                    case CoreBiomeType.Ocean:
                        return 0.1f; // Well below sea level
                    case CoreBiomeType.Coast:
                        return 0.25f; // Just above sea level
                    case CoreBiomeType.Plains:
                        return 0.4f; // Flat, moderate elevation
                    case CoreBiomeType.Forest:
                        return 0.45f; // Slightly rolling hills
                    case CoreBiomeType.Rainforest:
                        return 0.4f; // Generally flat to rolling
                    case CoreBiomeType.Desert:
                        return 0.35f; // Varied, but often flat
                    case CoreBiomeType.Mountains:
                        return 0.8f; // High elevation
                    case CoreBiomeType.Tundra:
                        return 0.3f; // Generally flat
                    case CoreBiomeType.Swamp:
                        return 0.2f; // Low, wet areas
                    default:
                        return 0.4f;
                }
            }

            private float GetBiomeHeightVariation(int x, int y, float2 normalizedPos, CoreBiomeType biome)
            {
                float2 pos = new float2(x, y);
                
                switch (biome)
                {
                    case CoreBiomeType.Ocean:
                        // Gentle ocean floor variation
                        float oceanNoise = noise.snoise(pos * NoiseScale * 0.5f + Seed) * 0.05f;
                        return oceanNoise;
                        
                    case CoreBiomeType.Coast:
                        // Gentle coastal variation
                        float coastNoise = noise.snoise(pos * NoiseScale * 0.8f + Seed) * 0.1f;
                        return coastNoise;
                        
                    case CoreBiomeType.Plains:
                        // Very gentle rolling hills
                        float plainsNoise = noise.snoise(pos * NoiseScale * 0.3f + Seed) * 0.08f;
                        return plainsNoise;
                        
                    case CoreBiomeType.Forest:
                        // Rolling forested hills
                        float forestNoise1 = noise.snoise(pos * NoiseScale * 0.4f + Seed) * 0.12f;
                        float forestNoise2 = noise.snoise(pos * NoiseScale * 0.8f + Seed + 100) * 0.06f;
                        return forestNoise1 + forestNoise2;
                        
                    case CoreBiomeType.Rainforest:
                        // Moderate hills with river valleys
                        float rainforestNoise = noise.snoise(pos * NoiseScale * 0.6f + Seed) * 0.15f;
                        return rainforestNoise;
                        
                    case CoreBiomeType.Desert:
                        // Sand dunes and rocky outcrops
                        float desertNoise1 = noise.snoise(pos * NoiseScale * 0.7f + Seed) * 0.2f;
                        float desertNoise2 = noise.snoise(pos * NoiseScale * 1.5f + Seed + 200) * 0.1f;
                        return desertNoise1 + desertNoise2;
                        
                    case CoreBiomeType.Mountains:
                        // Dramatic mountain peaks and valleys
                        float mountainNoise1 = noise.snoise(pos * NoiseScale * 0.3f + Seed) * 0.15f;
                        float mountainNoise2 = noise.snoise(pos * NoiseScale * 0.6f + Seed + 300) * 0.1f;
                        float mountainNoise3 = noise.snoise(pos * NoiseScale * 1.2f + Seed + 400) * 0.05f;
                        return mountainNoise1 + mountainNoise2 + mountainNoise3;
                        
                    case CoreBiomeType.Tundra:
                        // Gentle tundra with occasional hills
                        float tundraNoise = noise.snoise(pos * NoiseScale * 0.4f + Seed) * 0.1f;
                        return tundraNoise;
                        
                    case CoreBiomeType.Swamp:
                        // Very flat with small variations
                        float swampNoise = noise.snoise(pos * NoiseScale * 1.0f + Seed) * 0.03f;
                        return swampNoise;
                        
                    default:
                        return 0f;
                }
            }

            private void GenerateWaterFeatures(int index, int x, int y, float2 normalizedPos, CoreBiomeType biome, float height)
            {
                float water = 0f;
                
                switch (biome)
                {
                    case CoreBiomeType.Ocean:
                        water = 1.0f; // Full water coverage
                        break;
                        
                    case CoreBiomeType.Coast:
                        // Some coastal water features
                        float coastWaterNoise = noise.snoise(new float2(x, y) * NoiseScale * 2f + Seed + 3000);
                        water = coastWaterNoise > 0.3f ? 0.3f : 0f;
                        break;
                        
                    case CoreBiomeType.Swamp:
                        // Lots of standing water
                        float swampWaterNoise = noise.snoise(new float2(x, y) * NoiseScale * 1.5f + Seed + 3000);
                        water = swampWaterNoise > 0.2f ? 0.6f : 0.2f;
                        break;
                        
                    case CoreBiomeType.Rainforest:
                    case CoreBiomeType.Forest:
                        // Rivers and streams
                        float riverNoise = noise.snoise(new float2(x, y) * NoiseScale * 0.8f + Seed + 3000);
                        water = riverNoise > 0.7f ? 0.4f : 0f;
                        break;
                        
                    default:
                        // Occasional lakes in other biomes
                        float lakeNoise = noise.snoise(new float2(x, y) * NoiseScale * 0.5f + Seed + 3000);
                        water = lakeNoise > 0.85f ? 0.5f : 0f;
                        break;
                }
                
                WaterMap[index] = water;
            }

            private void GenerateResources(int index, float2 normalizedPos, CoreBiomeType biome, float height)
            {
                float resourceChance = GetBiomeResourceChance(biome);
                float resourceNoise = noise.snoise(normalizedPos * 50f + Seed + 2000);
                resourceNoise = (resourceNoise + 1) * 0.5f;

                if (resourceNoise > (1f - resourceChance))
                {
                    ResourceMap[index] = resourceNoise;
                    ResourceTypeMap[index] = DetermineBiomeResourceType(biome, resourceNoise);
                }
                else
                {
                    ResourceMap[index] = 0f;
                    ResourceTypeMap[index] = CoreResourceType.None;
                }
            }

            private float GetBiomeResourceChance(CoreBiomeType biome)
            {
                switch (biome)
                {
                    case CoreBiomeType.Mountains: return 0.4f; // Rich in stone and metal
                    case CoreBiomeType.Forest: return 0.3f; // Wood resources
                    case CoreBiomeType.Rainforest: return 0.25f; // Diverse resources
                    case CoreBiomeType.Desert: return 0.15f; // Sparse resources
                    case CoreBiomeType.Plains: return 0.2f; // Food and some materials
                    case CoreBiomeType.Coast: return 0.25f; // Food and trade resources
                    case CoreBiomeType.Swamp: return 0.1f; // Limited resources
                    case CoreBiomeType.Tundra: return 0.1f; // Very limited
                    case CoreBiomeType.Ocean: return 0.05f; // Minimal
                    default: return 0.1f;
                }
            }

            private CoreResourceType DetermineBiomeResourceType(CoreBiomeType biome, float resourceNoise)
            {
                switch (biome)
                {
                    case CoreBiomeType.Mountains:
                        return resourceNoise > 0.7f ? CoreResourceType.Metal : CoreResourceType.Stone;
                    case CoreBiomeType.Forest:
                    case CoreBiomeType.Rainforest:
                        return resourceNoise > 0.8f ? CoreResourceType.Food : CoreResourceType.Wood;
                    case CoreBiomeType.Plains:
                        return resourceNoise > 0.6f ? CoreResourceType.Food : CoreResourceType.Wood;
                    case CoreBiomeType.Coast:
                    case CoreBiomeType.Ocean:
                        return resourceNoise > 0.7f ? CoreResourceType.Food : CoreResourceType.Water;
                    case CoreBiomeType.Desert:
                        return resourceNoise > 0.8f ? CoreResourceType.Metal : CoreResourceType.Stone;
                    case CoreBiomeType.Swamp:
                return CoreResourceType.Water;
                    default:
                        return CoreResourceType.Stone;
                }
            }
        }

        [BurstCompile]
        private struct CollectChunksJob : IJob
        {
            public int Resolution;
            public NativeHashSet<int2> GeneratedChunks;

            public void Execute()
            {
                int chunkSize = 16;
                int numChunksX = Resolution / chunkSize;
                int numChunksY = Resolution / chunkSize;

                for (int cy = 0; cy < numChunksY; cy++)
                {
                    for (int cx = 0; cx < numChunksX; cx++)
                    {
                        GeneratedChunks.Add(new int2(cx, cy));
                    }
                }
            }
        }
    }
} 