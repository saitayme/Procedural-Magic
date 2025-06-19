using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using System.Collections.Generic;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using MathRandom = Unity.Mathematics.Random;
using CoreBiomeType = ProceduralWorld.Simulation.Core.BiomeType;
using SimTerrainData = ProceduralWorld.Simulation.Components.TerrainData;
using WorldTerrainData = ProceduralWorld.Simulation.Components.WorldTerrainData;
using ResourceType = ProceduralWorld.Simulation.Core.ResourceType;
using ProceduralWorld.Simulation.Visualization;
using UnityRandom = Unity.Mathematics.Random;

namespace ProceduralWorld.Simulation.Systems
{
    public struct CivilizationSpawnJobConfig
    {
        public NativeArray<ResourceType> ResourceTypes;
        public float minHeightForCivilization;
        public int initialCivilizationPopulation;
        public float initialCivilizationTechnology;
        public float initialCivilizationResources;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial class CivilizationSpawnSystem : SystemBase
    {
        private EntityQuery _terrainQuery;
        private EntityQuery _civilizationQuery;
        private EntityQuery _configQuery;
        private EntityQuery _historyQuery;
        private bool _isInitialized;
        private float _nextSpawn;
        private const float SPAWN_INTERVAL = 5.0f;
        private const int MAX_CIVILIZATIONS = 10;
        private const float MIN_CIVILIZATION_DISTANCE = 50f;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
        private MathRandom _random;
        private uint _seedCounter;

        protected override void OnCreate()
        {
            RequireForUpdate<Core.SimulationConfig>();
            RequireForUpdate<WorldTerrainData>();

            _terrainQuery = GetEntityQuery(
                ComponentType.ReadOnly<WorldTerrainData>()
            );
            
            _configQuery = GetEntityQuery(
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );

            _historyQuery = GetEntityQuery(
                ComponentType.ReadWrite<HistoricalEventData>()
            );

            _civilizationQuery = GetEntityQuery(
                ComponentType.ReadOnly<CivilizationData>()
            );

            _isInitialized = false;
            _nextSpawn = 0;
            _seedCounter = 1;
            _random = MathRandom.CreateFromIndex(_seedCounter);
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            
            Debug.Log("[CivilizationSpawnSystem] System created");
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnUpdate()
        {
            Debug.Log("[CivilizationSpawnSystem] OnUpdate called");
            
            if (!SystemAPI.HasSingleton<Core.SimulationConfig>())
            {
                Debug.Log("[CivilizationSpawnSystem] No SimulationConfig found");
                return;
            }

            var config = SystemAPI.GetSingleton<Core.SimulationConfig>();
            int maxCivilizations = config.maxCivilizations > 0 ? config.maxCivilizations : MAX_CIVILIZATIONS;
            Debug.Log($"[CivilizationSpawnSystem] Config: EnableCivilizationSpawning={config.EnableCivilizationSpawning}, EnableCivilizationSystem={config.EnableCivilizationSystem}, MaxCivilizations={maxCivilizations}");
            
            var civCount = _civilizationQuery.CalculateEntityCount();
            Debug.Log($"[CivilizationSpawnSystem] Civilization count at start of update: {civCount}");

            // ONLY spawn civilizations during initial setup (when there are 0 civilizations)
            // After that, new civilizations should only emerge through special events like revolts
            if (civCount > 0)
            {
                Debug.Log("[CivilizationSpawnSystem] Civilizations already exist - no automatic spawning. New civilizations should emerge through revolts/splits.");
                return;
            }

            // Initial setup - spawn starting civilizations
            if (!_isInitialized)
            {
                if (!config.EnableCivilizationSpawning)
                {
                    Debug.Log("[CivilizationSpawnSystem] Civilization spawning is disabled in config");
                    return;
                }
                _isInitialized = true;
                Debug.Log("[CivilizationSpawnSystem] Initialized and ready to spawn INITIAL civilizations");
            }

            // Only spawn if we have no civilizations (initial setup)
            if (civCount >= maxCivilizations)
            {
                Debug.Log("[CivilizationSpawnSystem] Initial civilization cap reached, spawning complete");
                return;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextSpawn)
                return;

            _nextSpawn = currentTime + SPAWN_INTERVAL;
            Debug.Log($"[CivilizationSpawnSystem] Attempting to spawn INITIAL civilization at time {currentTime}");

            // Get terrain data
            var terrainEntities = _terrainQuery.ToEntityArray(Allocator.Temp);
            if (terrainEntities.Length == 0)
            {
                Debug.Log("[CivilizationSpawnSystem] No terrain entities found");
                return;
            }

            var terrainData = EntityManager.GetComponentData<WorldTerrainData>(terrainEntities[0]);
            Debug.Log($"[CivilizationSpawnSystem] Terrain data found: Resolution={terrainData.Resolution}, WorldSize={terrainData.WorldSize}");

            // Defensive checks for terrain arrays
            if (!terrainData.HeightMap.IsCreated || terrainData.HeightMap.Length == 0 ||
                !terrainData.MoistureMap.IsCreated || terrainData.MoistureMap.Length == 0 ||
                !terrainData.TemperatureMap.IsCreated || terrainData.TemperatureMap.Length == 0 ||
                !terrainData.ResourceMap.IsCreated || terrainData.ResourceMap.Length == 0 ||
                !terrainData.BiomeMap.IsCreated || terrainData.BiomeMap.Length == 0)
            {
                Debug.LogWarning($"[CivilizationSpawnSystem] Terrain data arrays not initialized or empty. HeightMap: {terrainData.HeightMap.Length}, MoistureMap: {terrainData.MoistureMap.Length}, TemperatureMap: {terrainData.TemperatureMap.Length}, ResourceMap: {terrainData.ResourceMap.Length}, BiomeMap: {terrainData.BiomeMap.Length}");
                terrainEntities.Dispose();
                return;
            }

            // Divide map into 4x4 regions
            int regions = 4;
            int regionSize = terrainData.Resolution / regions;
            var regionCandidates = new List<(float score, int x, int z, float3 pos, CoreBiomeType biome)>[regions, regions];
            for (int ry = 0; ry < regions; ry++)
                for (int rx = 0; rx < regions; rx++)
                    regionCandidates[rx, ry] = new List<(float score, int x, int z, float3 pos, CoreBiomeType biome)>();

            float minHeight = 0.1f;
            float minFertility = 0.2f;
            float minResource = 0.1f;
            float minWaterProximity = 0.05f;
            int step = math.max(1, terrainData.Resolution / 64);
            float worldSize = terrainData.WorldSize;
            int candidatesPerRegion = 5; // Keep top 5 candidates per region

            for (int z = 0; z < terrainData.Resolution; z += step)
            {
                for (int x = 0; x < terrainData.Resolution; x += step)
                {
                    int idx = z * terrainData.Resolution + x;
                    float height = terrainData.HeightMap[idx];
                    float moisture = terrainData.MoistureMap[idx];
                    float temperature = terrainData.TemperatureMap[idx];
                    float resource = terrainData.ResourceMap[idx];
                    float water = terrainData.WaterMap.IsCreated && terrainData.WaterMap.Length > idx ? terrainData.WaterMap[idx] : 0f;
                    CoreBiomeType biome = terrainData.BiomeMap[idx];

                    // Allow civilizations in these biomes
                    bool isSuitableBiome = biome == CoreBiomeType.Plains || 
                                         biome == CoreBiomeType.Forest || 
                                         biome == CoreBiomeType.Rainforest || 
                                         biome == CoreBiomeType.Swamp || 
                                         biome == CoreBiomeType.Coast ||
                                         biome == CoreBiomeType.Mountains ||
                                         biome == CoreBiomeType.Desert;
                    if (!isSuitableBiome)
                        continue;
                    if (height < minHeight)
                        continue;

                    float fertility = (moisture + temperature) * 0.5f;
                    // Biome-specific requirements
                    bool meetsRequirements = true;
                    switch (biome)
                    {
                        case CoreBiomeType.Coast:
                            meetsRequirements = fertility >= minFertility * 0.8f && resource >= minResource * 0.8f;
                            break;
                        case CoreBiomeType.Mountains:
                            meetsRequirements = fertility >= minFertility * 0.6f && resource >= minResource * 1.2f;
                            break;
                        case CoreBiomeType.Desert:
                            meetsRequirements = fertility >= minFertility * 0.7f && resource >= minResource * 0.9f;
                            break;
                        case CoreBiomeType.Forest:
                        case CoreBiomeType.Rainforest:
                            meetsRequirements = fertility >= minFertility * 1.2f && resource >= minResource * 1.1f;
                            break;
                        default: // Plains, Swamp
                            meetsRequirements = fertility >= minFertility && resource >= minResource;
                            break;
                    }
                    if (!meetsRequirements)
                        continue;

                    // Score: weighted sum with biome-specific bonuses
                    float score = fertility * 2f + resource + moisture;
                    // Add biome-specific bonuses
                    switch (biome)
                    {
                        case CoreBiomeType.Forest:
                        case CoreBiomeType.Rainforest:
                            score += 0.8f;
                            break;
                        case CoreBiomeType.Mountains:
                            score += 0.6f;
                            break;
                        case CoreBiomeType.Desert:
                            score += 0.4f;
                            break;
                        case CoreBiomeType.Coast:
                            score += 0.5f;
                            break;
                    }
                    if (water > minWaterProximity)
                        score += 0.3f;

                    int rx = math.clamp(x / regionSize, 0, regions - 1);
                    int ry = math.clamp(z / regionSize, 0, regions - 1);
                    var candidate = (score, x, z, new float3((x - terrainData.Resolution / 2) * (worldSize / terrainData.Resolution), height * terrainData.HeightScale, (z - terrainData.Resolution / 2) * (worldSize / terrainData.Resolution)), biome);

                    // Add to region candidates if it's in the top N
                    var regionList = regionCandidates[rx, ry];
                    if (regionList.Count < candidatesPerRegion)
                    {
                        regionList.Add(candidate);
                    }
                    else
                    {
                        // Find worst score in region
                        int worstIdx = 0;
                        float worstScore = float.MaxValue;
                        for (int i = 0; i < regionList.Count; i++)
                        {
                            if (regionList[i].score < worstScore)
                            {
                                worstScore = regionList[i].score;
                                worstIdx = i;
                            }
                        }
                        // Replace if better
                        if (score > worstScore)
                        {
                            regionList[worstIdx] = candidate;
                        }
                    }
                }
            }

            // Collect candidates from all regions
            var candidateSpawns = new List<(float score, int x, int z, float3 pos, CoreBiomeType biome)>();
            for (int ry = 0; ry < regions; ry++)
                for (int rx = 0; rx < regions; rx++)
                    candidateSpawns.AddRange(regionCandidates[rx, ry]);

            // Shuffle candidates
            for (int i = candidateSpawns.Count - 1; i > 0; i--)
            {
                int j = _random.NextInt(0, i + 1);
                var temp = candidateSpawns[i];
                candidateSpawns[i] = candidateSpawns[j];
                candidateSpawns[j] = temp;
            }

            // Get existing civilization positions
            var existingCivs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var civPositions = new List<float3>();
            foreach (var civ in existingCivs)
                civPositions.Add(civ.Position);
            existingCivs.Dispose();

            // Select spawn points with more relaxed distance requirements
            int toSpawn = math.min(maxCivilizations - civCount, candidateSpawns.Count);
            float minDist = MIN_CIVILIZATION_DISTANCE * 0.8f; // Slightly relaxed distance
            var selectedSpawns = new List<(float3 pos, CoreBiomeType biome)>();
            
            foreach (var candidate in candidateSpawns)
            {
                bool tooClose = false;
                foreach (var pos in civPositions)
                {
                    if (math.distance(candidate.pos, pos) < minDist)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose)
                {
                    foreach (var sel in selectedSpawns)
                    {
                        if (math.distance(candidate.pos, sel.pos) < minDist)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }
                if (!tooClose)
                {
                    selectedSpawns.Add((candidate.pos, candidate.biome));
                    civPositions.Add(candidate.pos);
                    if (selectedSpawns.Count >= toSpawn)
                        break;
                }
            }

            // Spawn civilizations at selected points
            foreach (var (pos, biome) in selectedSpawns)
            {
                var civEntity = EntityManager.CreateEntity();
                
                // Generate random personality traits based on environment and starting conditions
                float environmentalStress = biome == CoreBiomeType.Desert || biome == CoreBiomeType.Mountains ? 0.3f : 0.1f;
                float baseAggressiveness = _random.NextFloat(1f, 4f) + environmentalStress;
                float baseDefensiveness = _random.NextFloat(2f, 5f);
                float baseGreed = _random.NextFloat(1f, 3f);
                float baseParanoia = _random.NextFloat(0.5f, 2f);
                float baseAmbition = _random.NextFloat(2f, 6f);
                
                // Create personality traits for epic naming
                var personality = new PersonalityTraits
                {
                    Aggressiveness = baseAggressiveness,
                    Defensiveness = baseDefensiveness,
                    Greed = baseGreed,
                    Paranoia = baseParanoia,
                    Ambition = baseAmbition,
                    Desperation = 0.5f,
                    Hatred = 0.2f,
                    Pride = _random.NextFloat(3f, 7f),
                    Vengefulness = _random.NextFloat(1f, 4f)
                };
                
                // Determine civilization type based on biome and personality
                CivilizationType civType = DetermineCivilizationType(biome, personality, _random);
                
                // Generate EPIC civilization name based on type, personality, and biome
                var civName = NameGenerator.GenerateEpicCivilizationName(civType, pos, biome, personality);
                
                var civilizationData = new CivilizationData
                {
                    Name = civName,
                    Type = civType,            // Set the civilization type
                    Population = 2000f,        // Higher starting population
                    Technology = 2.0f,         // Better starting tech
                    Resources = 3000.0f,       // More starting resources
                    Wealth = 1500.0f,          // Starting wealth to avoid poverty penalty
                    Stability = 0.8f,          // High starting stability for small civs
                    Military = 1.0f,           // Basic military
                    Trade = 0.5f,              // Some trade capability
                    Production = 1.0f,         // Basic production
                    Culture = 1.0f,            // Basic culture
                    Diplomacy = 1.0f,          // Basic diplomacy
                    Religion = 0.5f,           // Some religious influence
                    IsActive = true,           // Mark as active
                    Position = pos,
                    
                    // Initialize personality traits (use the ones we created for naming)
                    Aggressiveness = personality.Aggressiveness,
                    Defensiveness = personality.Defensiveness,
                    Greed = personality.Greed,
                    Paranoia = personality.Paranoia,
                    Ambition = personality.Ambition,
                    Desperation = personality.Desperation,
                    Hatred = personality.Hatred,
                    Pride = personality.Pride,
                    Vengefulness = personality.Vengefulness,
                    
                    // Initialize experience counters
                    TimesAttacked = 0,
                    TimesBetrayed = 0,
                    SuccessfulWars = 0,
                    LostWars = 0,
                    LastAttackedYear = -1f,
                    ResourceStressLevel = 0.1f,
                    HasBeenHumiliated = false
                };
                // Create AdaptivePersonalityData component
                var adaptivePersonality = new AdaptivePersonalityData
                {
                    BasePersonality = personality,
                    CurrentPersonality = personality,
                    TemporaryModifiers = new PersonalityTraits(), // Start with no modifiers
                    
                    // Initialize experience counters
                    SuccessfulWars = 0,
                    DefensiveVictories = 0,
                    TradeSuccesses = 0,
                    Betrayals = 0,
                    NaturalDisasters = 0,
                    CulturalAchievements = 0,
                    ReligiousEvents = 0,
                    DiplomaticVictories = 0,
                    
                    // Personality evolution settings
                    PersonalityFlexibility = _random.NextFloat(0.3f, 0.8f), // How adaptable this civ is
                    CurrentStress = 0.1f, // Low starting stress
                    TraumaResistance = _random.NextFloat(0.4f, 0.9f), // Resistance to negative changes
                    Stage = PersonalityEvolutionStage.Naive, // Start as naive
                    
                    // Memory system
                    PreviousPersonality = personality,
                    LastPersonalityChangeYear = 0f
                };

                EntityManager.AddComponentData(civEntity, civilizationData);
                EntityManager.AddComponentData(civEntity, adaptivePersonality);
                EntityManager.AddComponentData(civEntity, LocalTransform.FromPosition(pos));
                EntityManager.AddComponentData(civEntity, new Components.EntityMarker { Label = civName });
                EntityManager.AddComponentData(civEntity, new NameData {
                    Name = civName,
                    Position = pos,
                    Type = NameType.Civilization,
                    Significance = 1.0f,
                    SourceEntityId = civEntity
                });
                Debug.Log($"[CivilizationSpawnSystem] Spawned civilization '{civName}' at {pos} (biome: {biome})");
            }

            terrainEntities.Dispose();

            var resourceTypes = config.ResourceTypes;
            var jobConfig = new CivilizationSpawnJobConfig
            {
                ResourceTypes = resourceTypes,
                minHeightForCivilization = config.minHeightForCivilization,
                initialCivilizationPopulation = config.initialCivilizationPopulation,
                initialCivilizationTechnology = config.initialCivilizationTechnology,
                initialCivilizationResources = config.initialCivilizationResources
            };

            Debug.Log($"[CivilizationSpawnSystem] Candidate spawn count: {candidateSpawns.Count}");
        }

        private float3 FindSuitableSpawnLocation(ref SystemState state, WorldTerrainData terrainData)
        {
            var random = new Unity.Mathematics.Random((uint)SystemAPI.Time.ElapsedTime);
            var worldSize = terrainData.Resolution;
            var maxAttempts = 100;

            for (int i = 0; i < maxAttempts; i++)
            {
                var x = random.NextFloat(0, worldSize);
                var z = random.NextFloat(0, worldSize);
                var position = new float3(x, 0, z);

                if (IsValidSpawnLocation(position, terrainData))
                    return position;
            }

            return float3.zero;
        }

        private bool IsValidSpawnLocation(float3 position, WorldTerrainData terrainData)
        {
            // Check if position is too close to existing civilizations
            var existingCivilizations = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            foreach (var civ in existingCivilizations)
            {
                if (math.distance(position, civ.Position) < MIN_CIVILIZATION_DISTANCE)
                {
                    existingCivilizations.Dispose();
                    return false;
                }
            }
            existingCivilizations.Dispose();

            // Check if position is on suitable terrain
            var index = (int)position.z * terrainData.Resolution + (int)position.x;
            if (index < 0 || index >= terrainData.BiomeMap.Length)
                return false;

            var biome = terrainData.BiomeMap[index];
            return biome != Core.BiomeType.Ocean && biome != Core.BiomeType.None;
        }

        // EPIC CIVILIZATION TYPE DETERMINATION - Based on biome, personality, and randomness
        private CivilizationType DetermineCivilizationType(CoreBiomeType biome, PersonalityTraits personality, MathRandom random)
        {
            // Base weights for each type
            float militaryWeight = 1.0f;
            float techWeight = 1.0f;
            float religiousWeight = 1.0f;
            float tradeWeight = 1.0f;
            float culturalWeight = 1.0f;

            // Personality influences
            militaryWeight += personality.Aggressiveness * 0.3f + personality.Ambition * 0.2f;
            techWeight += (10f - personality.Aggressiveness) * 0.1f + personality.Ambition * 0.15f;
            religiousWeight += personality.Pride * 0.2f + (10f - personality.Greed) * 0.15f;
            tradeWeight += personality.Greed * 0.3f + personality.Ambition * 0.1f;
            culturalWeight += personality.Pride * 0.15f + (10f - personality.Aggressiveness) * 0.2f;

            // Biome influences
            switch (biome)
            {
                case CoreBiomeType.Mountains:
                    militaryWeight += 0.5f; // Mountain fortresses
                    techWeight += 0.3f; // Mining tech
                    break;
                case CoreBiomeType.Desert:
                    militaryWeight += 0.4f; // Harsh environment breeds warriors
                    tradeWeight += 0.6f; // Trade routes through deserts
                    break;
                case CoreBiomeType.Forest:
                    culturalWeight += 0.4f; // Forest wisdom
                    religiousWeight += 0.3f; // Nature worship
                    break;
                case CoreBiomeType.Coast:
                    tradeWeight += 0.7f; // Maritime trade
                    techWeight += 0.2f; // Navigation tech
                    break;
                case CoreBiomeType.Plains:
                    militaryWeight += 0.3f; // Open warfare
                    tradeWeight += 0.3f; // Trade routes
                    break;
                case CoreBiomeType.Tundra:
                    militaryWeight += 0.6f; // Survival of the fittest
                    religiousWeight += 0.2f; // Harsh gods
                    break;
                case CoreBiomeType.Swamp:
                    religiousWeight += 0.5f; // Mystical swamps
                    culturalWeight += 0.3f; // Unique traditions
                    break;
                case CoreBiomeType.Rainforest:
                    culturalWeight += 0.5f; // Rich biodiversity inspires culture
                    religiousWeight += 0.4f; // Spiritual connection to nature
                    break;
            }

            // Choose type based on weighted random
            float totalWeight = militaryWeight + techWeight + religiousWeight + tradeWeight + culturalWeight;
            float roll = random.NextFloat(0f, totalWeight);

            if (roll < militaryWeight)
                return CivilizationType.Military;
            roll -= militaryWeight;

            if (roll < techWeight)
                return CivilizationType.Technology;
            roll -= techWeight;

            if (roll < religiousWeight)
                return CivilizationType.Religious;
            roll -= religiousWeight;

            if (roll < tradeWeight)
                return CivilizationType.Trade;

            return CivilizationType.Cultural;
        }
    }
} 