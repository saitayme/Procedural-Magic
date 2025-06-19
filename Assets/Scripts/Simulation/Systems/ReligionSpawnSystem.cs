using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using UnityEngine;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using ProceduralWorld.Simulation.Utils;
using MathRandom = Unity.Mathematics.Random;

namespace ProceduralWorld.Simulation.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial struct ReligionSpawnSystem : ISystem
    {
        private EntityQuery _religionQuery;
        private EntityQuery _configQuery;
        private EntityQuery _terrainQuery;
        private bool _isInitialized;
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 1.0f;
        private ComponentTypeHandle<ReligionData> _religionHandle;
        private EntityQuery _civilizationQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _religionQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<ReligionData>(),
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );

            _civilizationQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CivilizationData>()
            );

            _terrainQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<WorldTerrainData>()
            );

            _isInitialized = false;
            _nextUpdate = 0;
            _religionHandle = state.GetComponentTypeHandle<ReligionData>(false);

            Debug.Log("[ReligionSpawnSystem] System created");
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<Core.SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<Core.SimulationConfig>();
                if (!config.EnableReligionSystem)
                    return;

                // Check if we need to spawn initial religions
                var religionCount = _religionQuery.CalculateEntityCount();
                var civilizationCount = _civilizationQuery.CalculateEntityCount();
                
                if (religionCount == 0 && civilizationCount > 0)
                {
                    Debug.Log($"[ReligionSpawnSystem] Spawning initial religions for {civilizationCount} civilizations");
                    
                    var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
                    var random = Unity.Mathematics.Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime);
                    
                    // Get civilization positions to spawn religions near them
                    var civEntities = _civilizationQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                    var civData = _civilizationQuery.ToComponentDataArray<CivilizationData>(Unity.Collections.Allocator.Temp);
                    
                    // Get terrain data for more grounded religion creation
                    WorldTerrainData terrainData = default;
                    bool hasTerrainData = false;
                    
                    if (_terrainQuery.HasSingleton<WorldTerrainData>())
                    {
                        terrainData = _terrainQuery.GetSingleton<WorldTerrainData>();
                        hasTerrainData = terrainData.BiomeMap.IsCreated;
                    }
                    
                    int religionsToSpawn = math.min(3, civilizationCount); // Spawn up to 3 religions
                    
                    for (int i = 0; i < religionsToSpawn; i++)
                    {
                        var civIndex = random.NextInt(0, civData.Length);
                        var civilization = civData[civIndex];
                        var basePosition = civilization.Position;
                        
                        // Add some randomness to the position
                        var offset = new float3(
                            random.NextFloat(-50f, 50f),
                            0,
                            random.NextFloat(-50f, 50f)
                        );
                        
                        var religionPosition = basePosition + offset;
                        var primaryBelief = (ReligionBelief)random.NextInt(1, 7);
                        
                        // Get actual biome from terrain data if available
                        var biome = hasTerrainData ? 
                            GetActualBiomeAtPosition(religionPosition, terrainData) : 
                            GetFallbackBiomeAtPosition(religionPosition);
                        
                        // Create religion context for more grounded naming
                        var religionContext = new ReligionContext
                        {
                            Position = religionPosition,
                            FoundingCivilization = civilization,
                            LocalBiome = biome,
                            PrimaryBelief = primaryBelief,
                            WorldAge = (float)SystemAPI.Time.ElapsedTime,
                            CivilizationTech = civilization.Technology,
                            CivilizationCulture = civilization.Culture,
                            CivilizationPopulation = civilization.Population
                        };
                        
                        var religion = new ReligionData
                        {
                            Name = ProceduralWorld.Simulation.Utils.NameGenerator.GenerateContextualReligionName(
                                religionContext, random),
                            Position = religionPosition,
                            PrimaryBelief = primaryBelief,
                            SecondaryBelief = (ReligionBelief)random.NextInt(1, 7),
                            Influence = random.NextFloat(0.1f, 1.0f),
                            Stability = random.NextFloat(0.5f, 1.0f),
                            Growth = random.NextFloat(0.0f, 0.1f),
                            Decline = random.NextFloat(0.0f, 0.05f),
                            FollowerCount = (int)(civilization.Population * random.NextFloat(0.1f, 0.8f)),
                            TempleCount = random.NextInt(1, 3),
                            HolySiteCount = random.NextInt(0, 2)
                        };
                        
                        var entity = ecb.CreateEntity();
                        ecb.AddComponent(entity, religion);
                        
                        Debug.Log($"[ReligionSpawnSystem] Spawned religion '{religion.Name}' at {religion.Position} " +
                                 $"(Biome: {biome}, Belief: {primaryBelief}, Followers: {religion.FollowerCount})");
                    }
                    
                    civEntities.Dispose();
                    civData.Dispose();
                }

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate)
                return;

            _nextUpdate = currentTime + UPDATE_INTERVAL;

            _religionHandle.Update(ref state);

            var job = new UpdateReligionJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Config = SystemAPI.GetSingleton<Core.SimulationConfig>(),
                ReligionHandle = _religionHandle
            };

            state.Dependency = job.ScheduleParallel(_religionQuery, state.Dependency);
        }

        [BurstCompile]
        private struct UpdateReligionJob : IJobChunk
        {
            public float DeltaTime;
            [ReadOnly] public Core.SimulationConfig Config;
            public ComponentTypeHandle<ReligionData> ReligionHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var religions = chunk.GetNativeArray(ref ReligionHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var religion = religions[i];

                    // Update religion values using temporary variables
                    float newInfluence = religion.Influence + Config.ReligionInfluenceRate * DeltaTime;
                    float newStability = religion.Stability + Config.ReligionStabilityRate * DeltaTime;

                    // Apply limits
                    religion.Influence = math.min(newInfluence, Config.MaxReligionInfluence);
                    religion.Stability = math.min(newStability, Config.MaxReligionStability);

                    religions[i] = religion;
                }
            }
        }

        private static Core.BiomeType GetActualBiomeAtPosition(float3 position, WorldTerrainData terrainData)
        {
            // Convert world position to terrain grid coordinates
            float worldSize = terrainData.WorldSize;
            int resolution = terrainData.Resolution;
            
            // Normalize position to [0,1] range
            float normalizedX = (position.x + worldSize * 0.5f) / worldSize;
            float normalizedZ = (position.z + worldSize * 0.5f) / worldSize;
            
            // Clamp to valid range
            normalizedX = math.clamp(normalizedX, 0f, 1f);
            normalizedZ = math.clamp(normalizedZ, 0f, 1f);
            
            // Convert to grid coordinates
            int gridX = (int)(normalizedX * (resolution - 1));
            int gridZ = (int)(normalizedZ * (resolution - 1));
            
            // Get biome from terrain data
            int index = gridZ * resolution + gridX;
            if (index >= 0 && index < terrainData.BiomeMap.Length)
            {
                return terrainData.BiomeMap[index];
            }
            
            return Core.BiomeType.Plains; // Fallback
        }

        private static Core.BiomeType GetFallbackBiomeAtPosition(float3 position)
        {
            // Simple biome determination based on position
            var hash = math.hash(new int3((int)position.x, 0, (int)position.z));
            var biomeIndex = hash % 9; // Number of main biomes
            
            return biomeIndex switch
            {
                0 => Core.BiomeType.Forest,
                1 => Core.BiomeType.Mountains,
                2 => Core.BiomeType.Desert,
                3 => Core.BiomeType.Ocean,
                4 => Core.BiomeType.Plains,
                5 => Core.BiomeType.Tundra,
                6 => Core.BiomeType.Swamp,
                7 => Core.BiomeType.Rainforest,
                8 => Core.BiomeType.Coast,
                _ => Core.BiomeType.Plains
            };
        }
    }

    [BurstCompile]
    public struct SpawnReligionsJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public int StartCount;
        public int MaxReligions;
        public float WorldSize;
        public int ReligionCount;
        public Unity.Mathematics.Random Random;

        public void Execute(int index)
        {
            var religion = new ReligionData
            {
                Name = new FixedString64Bytes($"Religion_{ReligionCount + index}"),
                PrimaryBelief = (ReligionBelief)((int)Random.NextInt(1, 7)), // Exclude None
                SecondaryBelief = (ReligionBelief)((int)Random.NextInt(1, 7)), // Exclude None
                Influence = Random.NextFloat(0.1f, 1.0f),
                Stability = Random.NextFloat(0.5f, 1.0f),
                Growth = Random.NextFloat(0.0f, 0.1f),
                Decline = Random.NextFloat(0.0f, 0.05f),
                Conversion = Random.NextFloat(0.0f, 0.1f),
                Resistance = Random.NextFloat(0.0f, 0.1f),
                Tolerance = Random.NextFloat(0.0f, 1.0f),
                Intolerance = Random.NextFloat(0.0f, 1.0f),
                Unity = Random.NextFloat(0.0f, 1.0f),
                Division = Random.NextFloat(0.0f, 1.0f),
                Peace = Random.NextFloat(0.0f, 1.0f),
                Conflict = Random.NextFloat(0.0f, 1.0f),
                Prosperity = Random.NextFloat(0.0f, 1.0f),
                Poverty = Random.NextFloat(0.0f, 1.0f),
                Health = Random.NextFloat(0.0f, 1.0f),
                Disease = Random.NextFloat(0.0f, 1.0f),
                Education = Random.NextFloat(0.0f, 1.0f),
                Ignorance = Random.NextFloat(0.0f, 1.0f),
                Knowledge = Random.NextFloat(0.0f, 1.0f),
                Wisdom = Random.NextFloat(0.0f, 1.0f),
                Folly = Random.NextFloat(0.0f, 1.0f),
                Intelligence = Random.NextFloat(0.0f, 1.0f),
                Stupidity = Random.NextFloat(0.0f, 1.0f),
                Genius = Random.NextFloat(0.0f, 1.0f),
                Idiocy = Random.NextFloat(0.0f, 1.0f),
                Talent = Random.NextFloat(0.0f, 1.0f),
                Mediocrity = Random.NextFloat(0.0f, 1.0f),
                Excellence = Random.NextFloat(0.0f, 1.0f),
                Inferiority = Random.NextFloat(0.0f, 1.0f),
                Superiority = Random.NextFloat(0.0f, 1.0f)
            };

            var entity = ECB.CreateEntity(index);
            ECB.AddComponent(index, entity, religion);
        }
    }

    // Context structure for more grounded religion generation
    public struct ReligionContext
    {
        public float3 Position;
        public CivilizationData FoundingCivilization;
        public Core.BiomeType LocalBiome;
        public ReligionBelief PrimaryBelief;
        public float WorldAge;
        public float CivilizationTech;
        public float CivilizationCulture;
        public float CivilizationPopulation;
    }
} 