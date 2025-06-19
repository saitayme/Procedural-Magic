using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial class EconomySpawnSystem : SystemBase
    {
        private EntityQuery _economyQuery;
        private EntityQuery _configQuery;
        private EntityQuery _civilizationQuery;
        private const float ECONOMY_UPDATE_INTERVAL = 1.0f;
        private float _nextEconomyUpdate;
        private bool _isInitialized;

        protected override void OnCreate()
        {
            _economyQuery = GetEntityQuery(ComponentType.ReadWrite<EconomyData>());
            _configQuery = GetEntityQuery(ComponentType.ReadOnly<SimulationConfig>());
            _civilizationQuery = GetEntityQuery(ComponentType.ReadOnly<CivilizationData>());
            _nextEconomyUpdate = 0f;
            _isInitialized = false;
        }

        protected override void OnUpdate()
        {
            if (!_configQuery.HasSingleton<SimulationConfig>())
                return;

            var config = _configQuery.GetSingleton<SimulationConfig>();
            if (!config.EnableEconomySystem)
                return;

            if (!_isInitialized)
            {
                // Check if we need to spawn initial economies
                var economyCount = _economyQuery.CalculateEntityCount();
                var civilizationCount = _civilizationQuery.CalculateEntityCount();
                
                if (economyCount == 0 && civilizationCount > 0)
                {
                    UnityEngine.Debug.Log($"[EconomySpawnSystem] Spawning initial economies for {civilizationCount} civilizations");
                    
                    var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
                    var random = Unity.Mathematics.Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime);
                    
                    // Get civilization positions to spawn economies near them
                    var civEntities = _civilizationQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                    var civData = _civilizationQuery.ToComponentDataArray<CivilizationData>(Unity.Collections.Allocator.Temp);
                    
                    int economiesToSpawn = math.min(4, civilizationCount); // Spawn up to 4 economies
                    
                    for (int i = 0; i < economiesToSpawn; i++)
                    {
                        var civIndex = random.NextInt(0, civData.Length);
                        var basePosition = civData[civIndex].Position;
                        
                        // Add some randomness to the position
                        var offset = new float3(
                            random.NextFloat(-30f, 30f),
                            0,
                            random.NextFloat(-30f, 30f)
                        );
                        
                        var economy = new EconomyData
                        {
                            Name = new FixedString128Bytes($"Economy_{i + 1}"),
                            Location = basePosition + offset,
                            Population = random.NextFloat(100f, 1000f),
                            Technology = random.NextFloat(0.1f, 1.0f),
                            Wealth = random.NextFloat(500f, 5000f),
                            Trade = random.NextFloat(0.1f, 1.0f),
                            Production = random.NextFloat(0.1f, 1.0f),
                            Stability = random.NextFloat(0.5f, 1.0f)
                        };
                        
                        var entity = ecb.CreateEntity();
                        ecb.AddComponent(entity, economy);
                        
                        UnityEngine.Debug.Log($"[EconomySpawnSystem] Spawned economy '{economy.Name}' at {economy.Location}");
                    }
                    
                    civEntities.Dispose();
                    civData.Dispose();
                }
                
                _isInitialized = true;
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            _nextEconomyUpdate -= deltaTime;

            if (_nextEconomyUpdate <= 0f)
            {
                _nextEconomyUpdate = ECONOMY_UPDATE_INTERVAL;

                var job = new EconomyUpdateJob
                {
                    DeltaTime = deltaTime
                };

                Dependency = job.ScheduleParallel(_economyQuery, Dependency);
            }
        }

        [BurstCompile]
        private partial struct EconomyUpdateJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref EconomyData economy)
            {
                var x = math.floor(economy.Location.x);
                var z = math.floor(economy.Location.z);
                economy.Location = new float3(x, 0, z);
            }
        }
    }
} 